using System.Text;

namespace PeakCraft.World;

/// <summary>Stable bridge identity for built-in and Addressables-loaded Unity scenes.</summary>
internal static class SceneWorldIdentity
{
    public static uint For(int buildIndex, string name)
    {
        // Preserve the established IDs and the legacy airport save (build index 3 -> 4).
        if (buildIndex >= 0) return (uint)buildIndex + 1u;
        uint hash = 2166136261u;
        // Addressables scenes have build index -1. Use exact, case-sensitive UTF-8 scene
        // names; the upper range keeps ordinary built-in scene IDs separate and nonzero.
        foreach (byte value in Encoding.UTF8.GetBytes(name ?? string.Empty))
            hash = unchecked((hash ^ value) * 16777619u);
        return hash | 0x80000000u;
    }
}
