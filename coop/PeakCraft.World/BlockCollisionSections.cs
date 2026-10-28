using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.World;

/// <summary>
/// Static PEAK physics for the exact collision shapes sent by the linked Minecraft client.
/// This root is independent of render visibility, textures and player ownership.
/// </summary>
internal sealed class BlockCollisionSections : IDisposable
{
    private sealed class Section
    {
        public GameObject Object = null!;
        public Mesh Mesh = null!;
        public MeshCollider Collider = null!;
        public int Boxes;
    }

    private static readonly HashSet<int> owned = new HashSet<int>();
    private readonly Dictionary<(int, int, int), Section> sections = new Dictionary<(int, int, int), Section>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private GameObject? root;
    private float lastInvalidLog = -100f;
    public int Count => sections.Count;
    public int BoxCount { get; private set; }

    // Corner numbering is xyz bit order in Unity coordinates. Every face points outwards.
    internal static readonly int[] BoxTriangles = {
        0,2,1, 1,2,3, 4,5,6, 5,7,6, 0,1,4, 1,5,4,
        2,6,3, 3,6,7, 0,4,2, 2,4,6, 1,3,5, 3,7,5
    };

    public static bool Owns(Collider collider) => collider != null && owned.Contains(collider.GetInstanceID());

    public void Set(ReadOnlySpan<byte> data, uint world, uint epoch)
    {
        if (!BlockCollisionPacket.TryRead(data, world, epoch, out int sx, out int sy, out int sz, out var boxes))
        {
            // Old packets queued before a scene/epoch transition are normal and simply discarded.
            if (data.Length < BlockCollisionPacket.HeaderBytes && Time.unscaledTime - lastInvalidLog > 10f)
            { lastInvalidLog = Time.unscaledTime; Plugin.Log.LogWarning("block physics: rejected truncated section"); }
            return;
        }
        var key = (sx, sy, sz);
        if (boxes.Length == 0) { Remove(key); return; }

        int layer = LayerMask.NameToLayer("Terrain");
        if (layer < 0) layer = LayerMask.NameToLayer("Map");
        if (layer < 0) throw new InvalidOperationException("PEAK Terrain/Map physics layers are unavailable.");
        vertices.Clear(); triangles.Clear();
        foreach (var box in boxes)
        {
            // Minecraft +Z is Unity -Z; construct corners after reflection so winding stays outward.
            int first = vertices.Count;
            for (int i = 0; i < 8; i++)
                vertices.Add(new Vector3((i & 1) == 0 ? box.MinX : box.MaxX,
                    (i & 2) == 0 ? box.MinY : box.MaxY, (i & 4) == 0 ? -box.MaxZ : -box.MinZ));
            foreach (int index in BoxTriangles) triangles.Add(first + index);
        }
        var mesh = new Mesh { name = $"MinePeak block collision {sx} {sy} {sz}", hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        if (root == null)
        {
            root = new GameObject("MinePeak block physics") { hideFlags = HideFlags.DontSave };
            UnityEngine.Object.DontDestroyOnLoad(root);
        }
        if (!sections.TryGetValue(key, out var section))
        {
            var go = new GameObject($"MinePeak block collider {sx} {sy} {sz}") { hideFlags = HideFlags.DontSave, layer = layer };
            go.transform.SetParent(root.transform, false);
            go.transform.position = Coords.ToUnity(sx * 16.0, sy * 16.0, sz * 16.0);
            section = new Section { Object = go, Collider = go.AddComponent<MeshCollider>() };
            owned.Add(section.Collider.GetInstanceID());
            sections[key] = section;
        }
        var previous = section.Mesh;
        BoxCount += boxes.Length - section.Boxes;
        section.Boxes = boxes.Length; section.Mesh = mesh;
        section.Collider.sharedMesh = mesh;
        if (previous != null) UnityEngine.Object.Destroy(previous);
    }

    private void Remove((int, int, int) key)
    {
        if (!sections.TryGetValue(key, out var section)) return;
        sections.Remove(key); BoxCount -= section.Boxes;
        // Destroy is deferred by Unity; disable immediately so breaking a block removes support now.
        if (section.Collider != null) { section.Collider.enabled = false; owned.Remove(section.Collider.GetInstanceID()); }
        if (section.Mesh != null) UnityEngine.Object.Destroy(section.Mesh);
        if (section.Object != null) UnityEngine.Object.Destroy(section.Object);
    }

    public void Clear()
    {
        foreach (var key in new List<(int, int, int)>(sections.Keys)) Remove(key);
    }

    public void Describe()
    {
        Plugin.Log.LogInfo($"block physics: {Count} sections / {BoxCount} exact Minecraft boxes; independent of rendering/mode");
        var c = Character.localCharacter;
        if (c == null) return;
        Vector3 eye = c.Center, direction = c.data.lookDirection.normalized;
        if (Physics.Raycast(eye, direction, out var hit, 20f, LayerMask.GetMask("Terrain", "Map"), QueryTriggerInteraction.Ignore))
            Plugin.Log.LogInfo($"block physics: ahead '{hit.collider.name}', minecraft {Owns(hit.collider)}, layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}, point {hit.point:F3}, normal {hit.normal:F3}, bounds {hit.collider.bounds}");
        if (Physics.Raycast(PeakCraft.Player.Follower.PeakFeet(c) + Vector3.up * .2f, Vector3.down, out hit, 20f,
            LayerMask.GetMask("Terrain", "Map"), QueryTriggerInteraction.Ignore))
            Plugin.Log.LogInfo($"block physics: support '{hit.collider.name}', minecraft {Owns(hit.collider)}, point {hit.point:F3}, distance {hit.distance:F3}");
    }

    public void Dispose()
    {
        Clear();
        if (root != null) UnityEngine.Object.Destroy(root);
        root = null;
    }
}
