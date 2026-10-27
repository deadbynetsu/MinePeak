using System;

namespace PeakCraft.Coop;

internal static class PosePacket
{
    public static bool Valid(object[] p)
    {
        if (p == null || p.Length != 8 || !(p[0] is int magic) || magic != CoopProto.Magic || !(p[7] is int flags) || flags < 0 || flags > 7) return false;
        for (int i = 1; i <= 6; i++)
            if (!(p[i] is float f) || float.IsNaN(f) || float.IsInfinity(f) || Math.Abs(f) > 30000000f) return false;
        return true;
    }
}
