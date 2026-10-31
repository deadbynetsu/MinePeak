using System;
using HarmonyLib;
using PeakCraft.Player;
using PeakCraft.World;

namespace PeakCraft.Patches;

/// <summary>Retain physics when the original game starts a native jump impulse.</summary>
[HarmonyPatch(typeof(CharacterMovement))]
internal static class NativeFlightPatch
{
    [HarmonyPatch("DoSuperJump")]
    [HarmonyPrefix]
    private static void SuperJumpPrefix(CharacterMovement __instance, float time)
        => Retain(__instance, time);

    [HarmonyPatch("JumpRpc")]
    [HarmonyPrefix]
    private static void JumpPrefix(CharacterMovement __instance)
        // JumpRpc owns its charge delay. NativeJump.TickNative observes the
        // charging flag and waits for the original jump and landing to finish.
        => Retain(__instance, 0f);

    internal static void Retain(CharacterMovement movement, float time)
    {
        var plugin = Plugin.Instance;
        Character character = Character.localCharacter;
        if (plugin == null || character == null || movement.character != character ||
            !plugin.Link.GuestAlive || plugin.CoopMode.IsViewer || LoadingScreenHandler.loading ||
            character.warping || PeakApi.IsDead(character.data) || character.data.passedOut ||
            character.data.fullyPassedOut || Ownership.PeakMenuOpen() || plugin.InputBridge.ScreenOpen ||
            float.IsNaN(time) || float.IsInfinity(time)) return;

        var owner = plugin.Ownership.State;
        if (owner != Owner.MinecraftOwns && owner != Owner.NativeControl) return;
        try
        {
            if (owner == Owner.MinecraftOwns)
            {
                // Incoming native area effects can start while the Minecraft
                // body is held. Release it before the original coroutine queues
                // its first force, rather than losing that force to the lock.
                var guest = plugin.Guest;
                var velocity = NativeJump.Velocity(in guest);
                plugin.Ownership.NativeOverride = true;
                plugin.Ownership.Update(true, in guest);
                if (plugin.Ownership.State != Owner.NativeControl) return;
                plugin.Follower.Release();
                foreach (Bodypart part in character.refs.ragdoll.partList)
                    part.Rig.linearVelocity = velocity;
                character.data.avarageVelocity = velocity;
            }
            plugin.Jump.RetainNativeFlight(character, time);
        }
        catch (Exception error)
        {
            // This hook never suppresses or replays the game's original action.
            Plugin.Log.LogWarning("jump: native flight retention failed: " + error.Message);
        }
    }
}
