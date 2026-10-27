using System;
using HarmonyLib;

namespace PeakCraft.Patches;

[HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
internal static class NativeHungerPatch
{
    private static void Prefix(CharacterAfflictions __instance, out float __state)
    {
        __state = float.NaN;
        var plugin = Plugin.Instance;
        if (plugin?.Vitals?.Engaged == true && plugin.Ownership.BodyFollows && __instance.character == Character.localCharacter)
        {
            __state = __instance.hungerPerSecond;
            __instance.hungerPerSecond = 0;
        }
    }

    private static Exception? Finalizer(CharacterAfflictions __instance, float __state, Exception? __exception)
    {
        if (!float.IsNaN(__state)) __instance.hungerPerSecond = __state;
        return __exception;
    }
}
