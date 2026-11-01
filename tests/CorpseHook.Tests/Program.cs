using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PeakCraft;
using PeakCraft.Patches;
using UnityEngine;

internal static class Program
{
    private static int passed;

    private static void Main()
    {
        Test("native corpse methods are observed only by void postfixes", () =>
        {
            var hooks = typeof(NativeCorpsePatch).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<HarmonyPostfix>() != null).ToArray();
            Check(hooks.Length == 3);
            Check(hooks.All(m => m.ReturnType == typeof(void)));
            var targets = hooks.Select(m => m.GetCustomAttribute<HarmonyPatch>()!).ToArray();
            Check(targets.Any(p => p.TargetType == typeof(Skelleton) && p.MethodName == nameof(Skelleton.SpawnSkelly)));
            Check(targets.Any(p => p.TargetType == typeof(Skelleton) && p.MethodName == nameof(Skelleton.SetCharacter)));
            Check(targets.Any(p => p.TargetType == typeof(Peak.PetrifiedScout) && p.MethodName == nameof(Peak.PetrifiedScout.RPC_SpawnPetrifiedScout)));
        });
        Test("a local skeleton registers its actual renderer set once per native callback", () =>
        {
            var owner = Character.localCharacter!;
            var renderers = new[] { new Renderer(), new Renderer() };
            var skeleton = new Skelleton { Renderers = renderers };
            Invoke("SkeletonSpawned", skeleton, owner);
            Check(skeleton.Scans == 1 && Plugin.Instance!.BodyVisibility.Calls.Count == 1);
            var call = Plugin.Instance!.BodyVisibility.Calls.Single();
            Check(ReferenceEquals(call.Owner, owner) && call.Renderers.SequenceEqual(renderers));
        });
        Test("native reassignment can register an existing local skeleton", () =>
        {
            var skeleton = new Skelleton { Renderers = new[] { new Renderer() } };
            Invoke("SkeletonAssigned", skeleton, Character.localCharacter!);
            Check(skeleton.Scans == 1 && Plugin.Instance!.BodyVisibility.Calls.Count == 1);
        });
        Test("remote corpses are rejected before any renderer hierarchy lookup", () =>
        {
            var remote = new Character();
            var skeleton = new Skelleton { ThrowOnScan = true };
            var piece = new GameObject { ThrowOnScan = true };
            var statue = new Peak.PetrifiedScout { spawnedFromCharacter = remote, ThrowOnScan = true, brokenBodyPieces = new[] { piece } };
            Invoke("SkeletonSpawned", skeleton, remote);
            Invoke("SkeletonAssigned", skeleton, remote);
            Invoke("StatueSpawned", statue);
            Check(skeleton.Scans == 0 && statue.Scans == 0 && piece.Scans == 0);
            Check(Plugin.Instance!.BodyVisibility.Calls.Count == 0 && Plugin.Log.Warnings == 0);
        });
        Test("unavailable plugin or local character performs no visual work", () =>
        {
            var owner = Character.localCharacter!;
            var skeleton = new Skelleton { ThrowOnScan = true };
            Plugin.Instance = null;
            Invoke("SkeletonSpawned", skeleton, owner);
            Plugin.Instance = new Plugin();
            Character.localCharacter = null;
            Invoke("SkeletonSpawned", skeleton, owner);
            Invoke("SkeletonAssigned", skeleton, null!);
            Check(skeleton.Scans == 0 && Plugin.Instance.BodyVisibility.Calls.Count == 0);
        });
        Test("a local statue registers inactive and detached pieces without moving them", () =>
        {
            var first = new GameObject { Renderers = new[] { new Renderer() } };
            var second = new GameObject { Renderers = new[] { new Renderer(), new Renderer() } };
            var statue = new Peak.PetrifiedScout {
                spawnedFromCharacter = Character.localCharacter,
                Renderers = new[] { new Renderer() }, brokenBodyPieces = new[] { first, null!, second }
            };
            Invoke("StatueSpawned", statue);
            Check(statue.Scans == 1 && first.Scans == 1 && second.Scans == 1);
            var calls = Plugin.Instance!.BodyVisibility.Calls;
            Check(calls.Count == 3 && calls.All(c => ReferenceEquals(c.Owner, Character.localCharacter)));
            Check(calls.SelectMany(c => c.Renderers).Count() == 4);
        });
        Test("a statue without broken pieces still registers its original renderer", () =>
        {
            var statue = new Peak.PetrifiedScout { spawnedFromCharacter = Character.localCharacter, Renderers = new[] { new Renderer() } };
            Invoke("StatueSpawned", statue);
            Check(statue.Scans == 1 && Plugin.Instance!.BodyVisibility.Calls.Count == 1);
        });
        Test("visual collection failure cannot escape the native spawn postfix", () =>
        {
            var skeleton = new Skelleton { ThrowOnScan = true };
            Invoke("SkeletonSpawned", skeleton, Character.localCharacter!);
            Check(skeleton.Scans == 1 && Plugin.Log.Warnings == 1 && Plugin.Instance!.BodyVisibility.Calls.Count == 0);
        });
        Test("registry failure is reported without replaying a native corpse spawn", () =>
        {
            var skeleton = new Skelleton();
            Plugin.Instance!.BodyVisibility.ThrowOnRegister = true;
            Invoke("SkeletonSpawned", skeleton, Character.localCharacter!);
            Check(skeleton.Scans == 1 && Plugin.Log.Warnings == 1);
        });
        Console.WriteLine($"PASS: {passed} native corpse registration hook tests");
    }

    private static void Invoke(string name, params object[] args)
    {
        try { typeof(NativeCorpsePatch).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args); }
        catch (TargetInvocationException error) when (error.InnerException != null) { throw error.InnerException; }
    }
    private static void Test(string name, Action run)
    {
        Plugin.Instance = new Plugin();
        Character.localCharacter = new Character();
        Plugin.Log.Warnings = 0;
        run(); passed++; Console.WriteLine("PASS: " + name);
    }
    private static void Check(bool valid) { if (!valid) throw new Exception("Native corpse registration regression"); }
}
