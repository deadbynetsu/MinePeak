using System;
using PeakCraft.Player;
using UnityEngine;

namespace PeakCraft.Dev;

/// <summary>Opt-in harness input only. Normal play never constructs or calls this helper.</summary>
internal sealed class DevNativeInput
{
    private Character? bound;
    private float until = -1f, forward, strafe, diagnoseUntil = -1f;
    private bool requested, primary, held, wasClimbing, releasePending;

    public void Set(Plugin plugin, float seconds, float moveForward, bool usePrimary, float moveStrafe)
    {
        if (plugin.Dev == null || plugin.Ownership.State != Owner.PeakOwns || Character.localCharacter == null)
            throw new InvalidOperationException("nativeinput requires the opted-in harness and native PEAK mode.");
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || float.IsNaN(moveForward) || float.IsInfinity(moveForward) ||
            float.IsNaN(moveStrafe) || float.IsInfinity(moveStrafe)) throw new ArgumentException("Finite input values required.");
        Character c = Character.localCharacter;
        if (bound != c) { Reset(); bound = c; }
        requested = true;
        until = Time.unscaledTime + Mathf.Clamp(seconds, 0f, 30f);
        forward = Mathf.Clamp(moveForward, -1f, 1f);
        strafe = Mathf.Clamp(moveStrafe, -1f, 1f);
        primary = usePrimary;
        wasClimbing = c.data.isClimbingAnything;
        diagnoseUntil = until + 1f;
        Plugin.Log.LogInfo($"nativeinput: scheduled {Mathf.Clamp(seconds, 0f, 30f):F2}s forward {forward:F2} strafe {strafe:F2} primary {primary}; stamina {c.data.currentStamina:F3}; feet {Follower.PeakFeet(c):F3}");
    }

    public void Apply(Plugin plugin, CharacterInput input)
    {
        Character c = Character.localCharacter;
        if (c == null || c != bound || c.input != input) { if (bound != c) Reset(); return; }
        bool eligible = plugin.Dev != null && plugin.Ownership.State == Owner.PeakOwns && !LoadingScreenHandler.loading &&
            !Ownership.PeakMenuOpen() && !c.data.dead && !c.data.passedOut && !c.data.fullyPassedOut;
        bool active = requested && eligible && Time.unscaledTime < until;
        if (!active && requested)
        {
            requested = false;
            diagnoseUntil = Time.unscaledTime + 1f;
            Plugin.Log.LogInfo($"nativeinput: releasing ({(eligible ? "timeout" : "native control ended")}); climbing {c.data.isClimbingAnything}; stamina {c.data.currentStamina:F3}; feet {Follower.PeakFeet(c):F3}");
        }
        // Other owners retain their own sampled input. Keep the virtual release until a
        // safe native sample resumes; a menu can leave the same character's grab alive.
        if (!eligible) { releasePending |= held; held = false; return; }
        if (releasePending)
        {
            releasePending = false;
            // A real primary hold on resume owns the grab and must keep its normal input.
            if (!input.usePrimaryIsPressed)
            {
                input.usePrimaryWasPressed = false;
                input.usePrimaryWasReleased = true;
                held = false;
                Plugin.Log.LogInfo("nativeinput: pending virtual release delivered after native control resumed");
                return;
            }
        }
        if (active || held)
        {
            bool down = active && primary;
            // These are ordinary sampled button edges, consumed by PEAK's own grab/release rules.
            // No TryClimb, stamina modification or climbing state assignment is performed here.
            input.usePrimaryWasPressed = down && !held;
            input.usePrimaryIsPressed = down;
            input.usePrimaryWasReleased = !down && held;
            input.movementInput = active ? Vector2.ClampMagnitude(new Vector2(strafe, forward), 1f) : Vector2.zero;
            held = down;
        }
        if (Time.unscaledTime <= diagnoseUntil && wasClimbing != c.data.isClimbingAnything)
        {
            wasClimbing = c.data.isClimbingAnything;
            var surface = c.data.climbHit.collider;
            Plugin.Log.LogInfo($"nativeinput: {(wasClimbing ? "native grab accepted" : "native climb detached")}; surface {(surface != null ? surface.name : "none")}; stamina {c.data.currentStamina:F3}; feet {Follower.PeakFeet(c):F3}");
        }
    }

    public void Reset()
    {
        bound = null; until = diagnoseUntil = -1f;
        requested = primary = held = wasClimbing = releasePending = false;
        forward = strafe = 0f;
    }
}
