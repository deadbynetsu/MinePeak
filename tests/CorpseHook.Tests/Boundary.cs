using System;
using System.Collections.Generic;
using System.Linq;

// A narrow spawn boundary. No scene search, physics, native death or item API
// is available to the linked production postfixes.
namespace UnityEngine
{
    internal class Object { }
    internal sealed class Renderer : Object { }
    internal class Component : Object
    {
        public Renderer[] Renderers = Array.Empty<Renderer>();
        public int Scans;
        public bool ThrowOnScan;
        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            Scans++;
            if (!includeInactive) throw new InvalidOperationException("Inactive death visuals were omitted");
            if (ThrowOnScan) throw new InvalidOperationException("Synthetic visual collection failure");
            return Renderers.Cast<T>().ToArray();
        }
    }
    internal sealed class GameObject : Component { }
}

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class HarmonyPatch : Attribute
    {
        public Type? TargetType;
        public string? MethodName;
        public HarmonyPatch() { }
        public HarmonyPatch(Type targetType, string methodName) { TargetType = targetType; MethodName = methodName; }
    }
    [AttributeUsage(AttributeTargets.Method)] internal sealed class HarmonyPostfix : Attribute { }
}

internal sealed class Character
{
    public static Character? localCharacter;
}
internal sealed class Skelleton : UnityEngine.Component
{
    public void SpawnSkelly(Character target) { }
    public void SetCharacter(Character target) { }
}
namespace Peak
{
    internal sealed class PetrifiedScout : UnityEngine.Component
    {
        public Character? spawnedFromCharacter;
        public UnityEngine.GameObject[]? brokenBodyPieces;
        public void RPC_SpawnPetrifiedScout(int characterId, bool killedByDagger) { }
    }
}
namespace PeakCraft
{
    internal sealed class Plugin
    {
        public static Plugin? Instance;
        public static readonly Logger Log = new();
        public readonly Registry BodyVisibility = new();
    }
    internal sealed class Registry
    {
        public readonly List<(Character Owner, UnityEngine.Renderer[] Renderers)> Calls = new();
        public bool ThrowOnRegister;
        public void RegisterCorpse(Character owner, UnityEngine.Renderer[] renderers)
        {
            if (ThrowOnRegister) throw new InvalidOperationException("Synthetic registry failure");
            Calls.Add((owner, renderers));
        }
    }
    internal sealed class Logger
    {
        public int Warnings;
        public void LogWarning(string value) => Warnings++;
    }
}
