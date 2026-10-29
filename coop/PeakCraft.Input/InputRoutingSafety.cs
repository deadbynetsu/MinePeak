using System;

namespace PeakCraft.Input;

/// <summary>UI state remains authoritative while focus temporarily stops input routing.</summary>
internal static class InputRoutingSafety
{
    public static bool MinecraftScreenOpen(bool minecraftPreference, bool guestAlive, uint flags)
        => minecraftPreference && guestAlive && (flags & 3u) == 3u;
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
