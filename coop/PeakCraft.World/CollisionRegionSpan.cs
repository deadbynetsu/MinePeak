using System;

namespace PeakCraft.World;

// Pure math shared by both sampled terrain and actual collider faces.
internal static class CollisionRegionSpan
{
    public static bool TryGet(float minY, float maxY, int windowMin, int windowMax, out int first, out int last)
    {
        first = last = 0;
        if (float.IsNaN(minY) || float.IsNaN(maxY) || float.IsInfinity(minY) || float.IsInfinity(maxY) || minY > maxY)
            return false;
        first = Math.Max(windowMin, (int)Math.Floor((minY - 0.001) / 8.0));
        last = Math.Min(windowMax, (int)Math.Floor((maxY + 0.001) / 8.0));
        return first <= last;
    }
}
