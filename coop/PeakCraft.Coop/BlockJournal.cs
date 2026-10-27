using System;
using System.Collections.Generic;

namespace PeakCraft.Coop;

/// <summary>Bounded latest-state journal. Lamport stamps make simultaneous edits converge without a permanent host.</summary>
internal sealed class BlockJournal
{
    public const int Capacity = 65536;
    private readonly Dictionary<(int x, int y, int z), BlockEdit> edits = new Dictionary<(int, int, int), BlockEdit>();
    private long clock;
    public int Count => edits.Count;
    public IEnumerable<BlockEdit> Values => edits.Values;

    public BlockEdit Local(string scene, int x, int y, int z, int state, string name, int actor)
    {
        if (clock == long.MaxValue) throw new InvalidOperationException("Co-op revision counter exhausted");
        return new BlockEdit(scene, x, y, z, state, name, ++clock, actor);
    }

    public bool Merge(in BlockEdit edit)
    {
        if (!edit.Valid) return false;
        clock = Math.Max(clock, edit.Revision);
        var key = (edit.X, edit.Y, edit.Z);
        if (edits.TryGetValue(key, out var previous))
        {
            if (edit.Revision < previous.Revision || (edit.Revision == previous.Revision && edit.Actor <= previous.Actor)) return false;
        }
        else if (edits.Count >= Capacity) return false;
        edits[key] = edit;
        return true;
    }

    public bool TryGet((int, int, int) key, out BlockEdit edit) => edits.TryGetValue(key, out edit);
    public void Clear() { edits.Clear(); clock = 0; }
}

internal readonly struct BlockEdit
{
    public readonly string Scene, Name;
    public readonly int X, Y, Z, State, Actor;
    public readonly long Revision;
    public BlockEdit(string scene, int x, int y, int z, int state, string name, long revision, int actor)
    { Scene = scene; X = x; Y = y; Z = z; State = state; Name = name; Revision = revision; Actor = actor; }

    public bool Valid => Scene != null && Scene.Length > 0 && Scene.Length <= 128 &&
        X >= -30000000 && X <= 30000000 && Z >= -30000000 && Z <= 30000000 && Y >= -1024 && Y <= 1023 &&
        State >= 0 && State <= 65535 && Revision > 0 && Revision < long.MaxValue && Actor > 0 && ValidName(Name);

    private static bool ValidName(string name)
    {
        if (name == null || name.Length == 0 || name.Length > 192 || name.IndexOf(':') <= 0) return false;
        foreach (char c in name)
            if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != ':' && c != '_' && c != '-' && c != '.' && c != '/') return false;
        return true;
    }

    public object[] Packet() => new object[] { CoopProto.Magic, CoopProto.Version, Scene, X, Y, Z, State, Name, Revision, Actor };

    public static bool TryParse(object[] p, out BlockEdit edit)
    {
        edit = default;
        if (p == null || p.Length != 10 || !(p[0] is int magic) || magic != CoopProto.Magic ||
            !(p[1] is int version) || version != CoopProto.Version || !(p[2] is string scene) ||
            !(p[3] is int x) || !(p[4] is int y) || !(p[5] is int z) || !(p[6] is int state) ||
            !(p[7] is string name) || !(p[8] is long revision) || !(p[9] is int actor)) return false;
        edit = new BlockEdit(scene, x, y, z, state, name, revision, actor);
        return edit.Valid;
    }
}
