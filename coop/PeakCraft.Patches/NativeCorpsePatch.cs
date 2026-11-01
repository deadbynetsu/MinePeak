using System;
using HarmonyLib;
using UnityEngine;

namespace PeakCraft.Patches;

/// <summary>Register local death visuals once, after the original native spawn.</summary>
[HarmonyPatch]
internal static class NativeCorpsePatch
{
    [HarmonyPatch(typeof(Skelleton), nameof(Skelleton.SpawnSkelly))]
    [HarmonyPostfix]
    private static void SkeletonSpawned(Skelleton __instance, Character __0)
        => Register(__instance, __0);

    [HarmonyPatch(typeof(Skelleton), nameof(Skelleton.SetCharacter))]
    [HarmonyPostfix]
    private static void SkeletonAssigned(Skelleton __instance, Character __0)
        => Register(__instance, __0);

    [HarmonyPatch(typeof(Peak.PetrifiedScout), nameof(Peak.PetrifiedScout.RPC_SpawnPetrifiedScout))]
    [HarmonyPostfix]
    private static void StatueSpawned(Peak.PetrifiedScout __instance)
    {
        if (__instance == null) return;
        Register(__instance, __instance.spawnedFromCharacter, __instance.brokenBodyPieces);
    }

    private static void Register(Component corpse, Character? owner, GameObject[]? detachedPieces = null)
    {
        var plugin = Plugin.Instance;
        // Capture while Viewer is selected too, so switching to Minecraft after
        // death can use the registry without searching the entire Unity scene.
        if (plugin == null || owner == null || owner != Character.localCharacter || corpse == null) return;
        try
        {
            plugin.BodyVisibility.RegisterCorpse(owner, corpse.GetComponentsInChildren<Renderer>(true));
            if (detachedPieces == null) return;
            foreach (var piece in detachedPieces)
                if (piece != null)
                    plugin.BodyVisibility.RegisterCorpse(owner, piece.GetComponentsInChildren<Renderer>(true));
        }
        catch (Exception error)
        {
            // Presentation must never interrupt the original death, corpse
            // creation or revival interaction that has already run.
            Plugin.Log.LogWarning("presentation: corpse visual registration failed: " + error.Message);
        }
    }
}
