using System;

namespace PeakCraft.Player;

/// <summary>
/// A native wall grab ending does not mean that the body has finished getting
/// onto the ledge. Wait for native physics to establish a settled landing.
/// All times are physics times, so rendering more frames cannot shorten the wait.
/// </summary>
internal sealed class NativeLandingGate
{
    private bool attached;
    private float detachedAt = float.NaN;
    private float supportedAt = float.NaN;

    public bool Observe(float physicsTime, bool climbing, bool grounded, float verticalSpeed)
    {
        if (climbing)
        {
            attached = true;
            detachedAt = supportedAt = float.NaN;
            return false;
        }
        if (!attached || float.IsNaN(physicsTime) || float.IsInfinity(physicsTime)) return false;
        if (float.IsNaN(detachedAt) || physicsTime < detachedAt)
            detachedAt = physicsTime;

        // A transient ground contact or the upward part of the native pull-up
        // must not freeze the ragdoll and hand it back to Minecraft mid-motion.
        if (!grounded || float.IsNaN(verticalSpeed) || Math.Abs(verticalSpeed) > 1f)
        {
            supportedAt = float.NaN;
            return false;
        }
        if (float.IsNaN(supportedAt) || physicsTime < supportedAt)
            supportedAt = physicsTime;
        return physicsTime - detachedAt >= 0.3f && physicsTime - supportedAt >= 0.2f;
    }

    public void Reset()
    {
        attached = false;
        detachedAt = supportedAt = float.NaN;
    }
}
