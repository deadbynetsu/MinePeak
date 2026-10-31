using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeakCraft.Player;

/// <summary>
/// An airborne Minecraft Space press can use an extra jump already granted by
/// PEAK. The original game consumes it, applies its impulse and raises its item
/// events; PEAK keeps the body until the landing is complete. Real native
/// super-jump coroutines can retain the same ownership until their landing.
/// </summary>
internal sealed class NativeJump
{
    private readonly NativeLandingGate landing = new();
    private Character? bound;
    private bool spaceHeld, pending, requested, devJump;
    private Vector3 entryVelocity;
    private double forceUntil;
    private bool retainedFlight;

    public bool WantsNative => requested;
    public bool RetainingFlight => requested && retainedFlight;

    public void UpdateRequest(bool minecraftMode, bool screenOpen, Character c,
        in Proto.GuestState guest, Owner owner)
    {
        bool syntheticPress = devJump;
        devJump = false;
        bool down = Keyboard.current != null && Keyboard.current[Key.Space].isPressed;
        bool pressed = syntheticPress || (Application.isFocused && down && !spaceHeld);
        spaceHeld = down;

        bool eligible = minecraftMode && !screenOpen && c != null && !c.warping && !LoadingScreenHandler.loading &&
            !PeakApi.IsDead(c.data) && !c.data.passedOut && !c.data.fullyPassedOut;
        if (!eligible)
        {
            Cancel();
            return;
        }
        if (bound != c) { Cancel(); bound = c; }
        if (requested || owner != Owner.MinecraftOwns) return;

        uint flags = guest.flags;
        if ((flags & ((uint)Proto.GuestFlags.MovementReady | (uint)Proto.GuestFlags.InWorld)) !=
            ((uint)Proto.GuestFlags.MovementReady | (uint)Proto.GuestFlags.InWorld) ||
            (flags & ((uint)Proto.GuestFlags.Creative | (uint)Proto.GuestFlags.Flying |
                      (uint)Proto.GuestFlags.Swimming | (uint)Proto.GuestFlags.ScreenOpen |
                      (uint)Proto.GuestFlags.Dead)) != 0) return;

        if ((flags & (uint)Proto.GuestFlags.OnGround) != 0)
        {
            // Native ground evaluation is intentionally disabled for the held
            // ragdoll. Restore its real passive allowance from the afflictions
            // only when Minecraft actually reports a landing.
            c.data.extraJumps = c.refs.afflictions.totalExtraJumps;
            c.data.jumpsRemaining = 1;
            if (pressed) c.data.sinceJump = 0f;
            return;
        }
        if (!pressed || c.data.extraJumps <= 0 || c.refs.afflictions.totalExtraJumps <= 0 ||
            c.data.chargingJump || c.data.sinceJump < 0.1f || !c.CheckJump()) return;

        entryVelocity = Velocity(in guest);
        requested = pending = true;
        landing.Reset();
        Plugin.Log.LogInfo("jump: native extra jump requested from Minecraft");
    }

    /// <summary>Call after Follower.Update has released the native body.</summary>
    public void TickNative(Character c)
    {
        if (!requested || c == null || Plugin.Instance.Ownership.State != Owner.NativeControl) return;
        if (pending)
        {
            pending = false;
            foreach (Bodypart part in c.refs.ragdoll.partList)
                part.Rig.linearVelocity = entryVelocity;
            c.data.avarageVelocity = entryVelocity;
            c.data.isGrounded = false;
            c.data.groundedFor = 0f;
            c.data.sinceGrounded = Mathf.Max(c.data.sinceGrounded, 0.21f);
            // The ordinary ground jump was already performed in Minecraft.
            // Preserve PEAK's remaining bonus count and let JumpRpc consume it.
            c.data.jumpsRemaining = 0;
            int before = c.data.extraJumps;
            c.refs.movement.TryToJump();
            if (c.data.extraJumps >= before)
            {
                Plugin.Log.LogInfo("jump: native extra jump rejected by PEAK");
                Cancel();
                return;
            }
            landing.Observe(Time.fixedTime, true, false, 0f);
            Plugin.Log.LogInfo($"jump: native extra jump accepted; remaining {c.data.extraJumps}");
            return;
        }
        bool nativeAction = Time.fixedTime < forceUntil || c.data.chargingJump ||
            c.data.isClimbingAnything || c.data.currentClimbHandle != null;
        if (landing.Observe(Time.fixedTime, nativeAction, c.data.isGrounded, c.data.avarageVelocity.y))
        {
            Plugin.Log.LogInfo("jump: native landing settled; returning to Minecraft");
            Cancel();
        }
    }

    // The dev harness requests the same edge and native validation as Space.
    public void DevJump() => devJump = true;

    /// <summary>
    /// Called only when the original local PEAK jump method is about to execute.
    /// Its own coroutine and costs stay native; retaining ownership prevents
    /// releasing B/V from freezing a charge delay or remaining force interval.
    /// </summary>
    public void RetainNativeFlight(Character c, float forceSeconds)
    {
        if (c == null || float.IsNaN(forceSeconds) || float.IsInfinity(forceSeconds)) return;
        if (bound != c) Cancel();
        bound = c;
        requested = true;
        pending = false;
        retainedFlight = true;
        forceUntil = System.Math.Max(forceUntil, (double)Time.fixedTime + System.Math.Max(0f, forceSeconds));
        landing.Reset();
        landing.Observe(Time.fixedTime, true, false, 0f);
        Plugin.Log.LogInfo("jump: retaining original native impulse through landing");
    }

    public void Reset()
    {
        Cancel();
        bound = null;
        spaceHeld = devJump = false;
    }

    private void Cancel()
    {
        requested = pending = false;
        retainedFlight = false;
        forceUntil = 0;
        landing.Reset();
    }

    internal static Vector3 Velocity(in Proto.GuestState guest)
    {
        float seconds = guest.tickMs * 0.001f;
        if (seconds < 0.001f || seconds > 0.25f) return Vector3.zero;
        var velocity = new Vector3((float)((guest.curX - guest.prevX) / seconds),
            (float)((guest.curY - guest.prevY) / seconds),
            (float)((guest.prevZ - guest.curZ) / seconds));
        if (float.IsNaN(velocity.x) || float.IsInfinity(velocity.x) ||
            float.IsNaN(velocity.y) || float.IsInfinity(velocity.y) ||
            float.IsNaN(velocity.z) || float.IsInfinity(velocity.z)) return Vector3.zero;
        return Vector3.ClampMagnitude(velocity, 30f);
    }
}
