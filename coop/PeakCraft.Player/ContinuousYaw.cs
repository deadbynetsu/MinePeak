using System;

namespace PeakCraft.Player;

/// <summary>
/// Keeps the local heading continuous: Minecraft's first-person hand inertia
/// interpolates yaw as an ordinary number, rather than a wrapped angle.
/// </summary>
internal sealed class ContinuousYaw
{
    public float Value { get; private set; }

    public void Turn(float degrees)
    {
        if (float.IsNaN(degrees) || float.IsInfinity(degrees)) return;
        var next = Value + degrees;
        if (!float.IsNaN(next) && !float.IsInfinity(next)) Value = next;
    }

    /// <summary>Lift a native/absolute heading onto the nearest local revolution.</summary>
    public void Follow(float degrees)
    {
        if (float.IsNaN(degrees) || float.IsInfinity(degrees)) return;
        var delta = (degrees - (double)Value) % 360.0;
        if (delta > 180.0) delta -= 360.0;
        if (delta < -180.0) delta += 360.0;
        Value += (float)delta;
    }
}
