using System;

namespace PeakCraft.Input;

/// <summary>UI state remains authoritative while focus temporarily stops input routing.</summary>
internal static class InputRoutingSafety
{
    public static bool MinecraftScreenOpen(bool minecraftPreference, bool guestAlive, uint flags)
        => minecraftPreference && guestAlive && (flags & 3u) == 3u;

    // BackpackWheel remains open only while the interaction action is held.
    // Opening that wheel changes ownership to PeakMenu on the very next frame.
    public static bool RoutePeakInteract(bool nativeControl, bool backpackWheel, bool minecraftScreen, bool focused)
        => focused && !minecraftScreen && (nativeControl || backpackWheel);
}

/// <summary>A safety release is never forgotten when the shared input ring is full.</summary>
internal sealed class InputReleaseBarrier
{
    public bool Pending { get; private set; }

    public void Request() => Pending = true;

    public bool Flush(Func<bool> sendRelease)
    {
        if (!Pending) return true;
        if (!sendRelease()) return false;
        Pending = false;
        return true;
    }
}
