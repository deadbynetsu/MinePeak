using BepInEx.Configuration;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeakCraft.Player;

/// <summary>
/// Requests a temporary PEAK-owned body while the Minecraft preference stays on.
/// The real CharacterClimbing component owns wall tests, motion, stamina and release.
/// </summary>
internal sealed class NativeClimb
{
    private readonly ConfigEntry<Key> key;
    private bool physicalHeld;
    private bool inputHeld;
    private bool attached;
    private bool settling;
    private bool jumpRelease;
    private readonly NativeLandingGate returnGate = new();
    private bool preexistingClimb;
    private bool blockedUntilRelease;
    private bool releasePending;
    private bool returnTeleport;
    private float releaseFixedTime;
    private float nextAttempt;
    private float devUntil = -1f;
    private float devForward;
    private Character? bound;

    public bool Requested { get; private set; }
    public bool Active { get; private set; }
    public bool WantsNative => Requested || releasePending;
    public string KeyName => key.Value.ToString();

    public NativeClimb(ConfigFile config)
    {
        key = config.Bind("Keys", "PeakClimb", Key.V,
            "Hold to use PEAK's real climbing while Minecraft mode remains enabled. WASD climbs; Space releases. Let go to return to Minecraft.");
    }

    public void UpdateRequest(bool minecraftMode, bool screenOpen, Character c)
    {
        bool eligible = minecraftMode && c != null && !LoadingScreenHandler.loading &&
            !PeakApi.IsDead(c.data) && !c.data.passedOut && !c.data.fullyPassedOut;
        if (!eligible)
        {
            // Changing modes can remove the virtual V-held primary action without a real
            // mouse-release edge. Release only a climb started by this helper; a native
            // climb already in progress, or supported by a physical primary action,
            // keeps its normal game controls.
            if (!minecraftMode && Active && !preexistingClimb && bound == c && c != null &&
                (c.data.isClimbingAnything || c.data.currentClimbHandle != null) &&
                !PhysicalPrimaryHeld() && c.refs.climbing != null)
            {
                c.refs.climbing.StopAnyClimbing();
                Plugin.Log.LogInfo("climb: virtual grab released as Minecraft mode ended");
            }
            Reset();
            return;
        }
        if (bound != null && bound != c) Reset();
        bound = c;

        bool down = !screenOpen && Application.isFocused &&
            ((Keyboard.current != null && Keyboard.current[key.Value].isPressed) || Time.unscaledTime < devUntil);
        // Tests use the opt-in dev harness without taking global keyboard focus.
        if (!screenOpen && Time.unscaledTime < devUntil) down = true;

        if (!down) blockedUntilRelease = false;
        if (down && !physicalHeld && !blockedUntilRelease)
        {
            Requested = true;
            releasePending = false;
            Active = attached = false;
            settling = jumpRelease = false;
            returnGate.Reset();
            preexistingClimb = c.data.isClimbingAnything || c.data.currentClimbHandle != null;
            nextAttempt = 0;
            Plugin.Log.LogInfo("climb: PEAK control requested; hold " + KeyName + ", use WASD and Space");
        }
        if (!down && physicalHeld && Requested)
            BeginReturn(c, true, "key released");
        physicalHeld = down;

        if (releasePending && Time.fixedTime > releaseFixedTime)
        {
            releasePending = false;
            Active = false;
            returnTeleport = true;
        }
    }

    /// <summary>Call after Follower.Update has released the PEAK ragdoll.</summary>
    public void TickNative(Character c)
    {
        if (!Requested || c == null || Plugin.Instance.Ownership.State != Owner.NativeControl) return;
        Active = true;
        if (jumpRelease)
        {
            // CharacterClimbing has already received the normal Space input.
            // Keep its release behavior rather than treating it as a ledge.
            BeginReturn(c, false, "Space released the climb");
            return;
        }
        if (c.data.isClimbingAnything || c.data.currentClimbHandle != null)
        {
            if (!attached)
                Plugin.Log.LogInfo($"climb: native grab accepted; stamina {c.data.currentStamina:F3}; feet {Follower.PeakFeet(c):F3}");
            attached = true;
            settling = false;
            returnGate.Observe(Time.fixedTime, true, false, 0f);
            return;
        }
        if (attached)
        {
            // PEAK clears its climb flag at a walkable top before its ordinary
            // gravity/standing physics have finished carrying the body over it.
            // Keep PEAK ownership until landing; a fall also remains native while
            // V is held. Explicit release/menu cancellation still returns below.
            if (!settling)
            {
                settling = true;
                Plugin.Log.LogInfo("climb: grab ended; waiting for a settled native landing");
            }
            if (returnGate.Observe(Time.fixedTime, false, c.data.isGrounded, c.data.avarageVelocity.y))
                BeginReturn(c, false, "native landing settled");
            return;
        }
        if (Time.unscaledTime < nextAttempt) return;
        nextAttempt = Time.unscaledTime + 0.15f;
        // This is the game's own grab path. It still validates wall angle, distance,
        // ownership, rope/vine state and native out-of-stamina restrictions.
        c.refs.climbing.TryClimb();
        if (c.data.isClimbingAnything || c.data.currentClimbHandle != null)
        {
            attached = true;
            returnGate.Observe(Time.fixedTime, true, false, 0f);
            Plugin.Log.LogInfo($"climb: native grab accepted; stamina {c.data.currentStamina:F3}; feet {Follower.PeakFeet(c):F3}");
        }
    }

    /// <summary>Call last in the local CharacterInput.Sample postfix while native control is granted.</summary>
    public void ApplyInput(CharacterInput input)
    {
        if (!WantsNative) { inputHeld = false; return; }
        if (Requested && attached && input.jumpWasPressed) jumpRelease = true;
        // EquipSlot(None) can take a short coroutine to pocket/drop a bulky item.
        // The virtual climb click must not fire that item's primary action meanwhile.
        bool hold = Requested && (bound == null || bound.data.currentItem == null);
        bool nativeRelease = preexistingClimb && input.usePrimaryWasReleased;
        input.usePrimaryWasPressed = hold && !inputHeld;
        input.usePrimaryIsPressed = hold;
        input.usePrimaryWasReleased = (!hold && inputHeld) || nativeRelease;
        inputHeld = hold;
        if (hold && Time.unscaledTime < devUntil && Mathf.Abs(devForward) > 0.001f)
            input.movementInput = new Vector2(input.movementInput.x, devForward);
    }

    private void BeginReturn(Character c, bool stop, string reason)
    {
        if (stop && Active && c != null && c.refs.climbing != null &&
            Plugin.Instance.Ownership.State == Owner.NativeControl)
            c.refs.climbing.StopAnyClimbing();
        Requested = false;
        jumpRelease = false;
        returnGate.Reset();
        releasePending = Active;
        releaseFixedTime = Time.fixedTime;
        blockedUntilRelease = physicalHeld;
        if (!releasePending) returnTeleport = true;
        Plugin.Log.LogInfo("climb: returning to Minecraft after " + reason);
    }

    /// <summary>Owner.NativeControl -> Handoff must also issue a fresh teleport sequence.</summary>
    public bool ConsumeReturnTeleport()
    {
        bool value = returnTeleport;
        returnTeleport = false;
        return value;
    }

    public void DevHold(float seconds, float forward = 0f)
    {
        devUntil = Time.unscaledTime + Mathf.Clamp(seconds, 0f, 30f);
        devForward = Mathf.Clamp(forward, -1f, 1f);
    }

    public void Reset()
    {
        Requested = Active = physicalHeld = inputHeld = attached = preexistingClimb = blockedUntilRelease = releasePending = returnTeleport = false;
        settling = jumpRelease = false;
        returnGate.Reset();
        bound = null;
        nextAttempt = 0;
        devUntil = -1f;
        devForward = 0;
    }

    private static bool PhysicalPrimaryHeld()
    {
        return CharacterInput.action_usePrimary?.IsPressed() == true;
    }
}
