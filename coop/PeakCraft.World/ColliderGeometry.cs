using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.World;

/// <summary>
/// Adds actual collider surfaces to the sampled terrain. Vertical rays alone miss
/// thin walls, rotated boxes and geometry that falls between two 0.5 m samples.
/// All data stays in this process; unreadable mesh buffers are read asynchronously.
/// </summary>
internal sealed class ColliderGeometry
{
    private sealed class MeshData
    {
        public Vector3[]? Vertices;
        public int[]? Indices;
        public byte[]? VertexBytes, IndexBytes;
        public bool Failed;
        public uint Generation;
        public int Stride, Offset, VertexCount;
        public IndexFormat IndexFormat;
        public SubMeshDescriptor[] Submeshes = Array.Empty<SubMeshDescriptor>();
        public bool Ready => Vertices != null && Indices != null;
    }

    private sealed class WorldMesh
    {
        public Mesh? Mesh;
        public Matrix4x4 Matrix;
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public readonly Dictionary<long, int[]> Columns = new Dictionary<long, int[]>();
    }

    private readonly Dictionary<int, MeshData> meshes = new Dictionary<int, MeshData>();
    private readonly Dictionary<int, WorldMesh> worlds = new Dictionary<int, WorldMesh>();
    private readonly List<int> columnIndices = new List<int>();
    private Collider[] overlaps = new Collider[256];
    private Character? maskCharacter;
    private int cachedMask;
    private int readyMeshes, failedMeshes;
    private uint generation;
    public uint Revision { get; private set; }
    public int LastColliderCount { get; private set; }
    public int ReadyMeshes => readyMeshes;
    public int FailedMeshes => failedMeshes;

    public void Clear()
    {
        meshes.Clear(); worlds.Clear(); maskCharacter = null;
        generation++;
        readyMeshes = failedMeshes = 0;
        Revision++;
    }

    private int QueryMask(int baseMask)
    {
        Character character = Character.localCharacter;
        if (character == null || character.refs.ragdoll == null) return baseMask;
        if (maskCharacter == character) return cachedMask;
        int layer = -1;
        foreach (Collider body in character.refs.ragdoll.colliderList)
            if (body != null && !body.isTrigger) { layer = body.gameObject.layer; break; }
        cachedMask = baseMask;
        if (layer >= 0)
            for (int i = 0; i < 32; i++)
                if (!Physics.GetIgnoreLayerCollision(layer, i)) cachedMask |= 1 << i;
        maskCharacter = character;
        return cachedMask;
    }

    public bool AppendColumn(int rx, int rz, float bottom, float top, int baseMask, Action<Vector3, Vector3, Vector3> emit)
    {
        float minX = rx * 8f, minZ = -((rz + 1) * 8f);
        Vector3 center = new Vector3(minX + 4f, (bottom + top) * 0.5f, minZ + 4f);
        Vector3 half = new Vector3(4.01f, (top - bottom) * 0.5f + 0.01f, 4.01f);
        int count;
        while (true)
        {
            count = Physics.OverlapBoxNonAlloc(center, half, overlaps, Quaternion.identity, QueryMask(baseMask), QueryTriggerInteraction.Ignore);
            if (count < overlaps.Length || overlaps.Length >= 8192) break;
            overlaps = new Collider[overlaps.Length * 2];
        }
        bool pending = false;
        LastColliderCount = 0;
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            if (!SolidWorldCollider(collider)) continue;
            LastColliderCount++;
            if (collider is BoxCollider box)
            {
                Box(box, emit);
            }
            else if (collider is SphereCollider sphere)
            {
                Vector3 scale = sphere.transform.lossyScale;
                float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                Capsule(sphere.transform.TransformPoint(sphere.center), Vector3.up, radius, 0f, emit);
            }
            else if (collider is CapsuleCollider capsule)
            {
                Vector3 scale = capsule.transform.lossyScale;
                int direction = capsule.direction;
                float axialScale = Mathf.Abs(scale[direction]);
                float radialScale = Mathf.Max(Mathf.Abs(scale[(direction + 1) % 3]), Mathf.Abs(scale[(direction + 2) % 3]));
                Vector3 localAxis = Vector3.zero; localAxis[direction] = 1f;
                float radius = capsule.radius * radialScale;
                Capsule(capsule.transform.TransformPoint(capsule.center), capsule.transform.TransformDirection(localAxis).normalized,
                    radius, Mathf.Max(0, capsule.height * axialScale * 0.5f - radius), emit);
            }
            else if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
            {
                MeshData data = GetMesh(meshCollider.sharedMesh);
                if (!data.Ready) { pending |= !data.Failed; continue; }
                EmitMeshColumn(meshCollider, data, rx, rz, minX, minZ, bottom, top, emit);
            }
            // TerrainCollider remains covered by the existing multilayer height sampling.
        }
        Array.Clear(overlaps, 0, count);
        return pending;
    }

    private static bool SolidWorldCollider(Collider collider)
    {
        if (collider == null || !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy) return false;
        if (BlockCollisionSections.Owns(collider)) return false;
        if (collider.GetComponentInParent<Character>() != null) return false;
        if (collider.GetComponentInParent<Bodypart>() != null) return false;
        Rigidbody body = collider.attachedRigidbody;
        if (body != null && !body.isKinematic) return false;
        Item item = collider.GetComponentInParent<Item>();
        return item == null || item.holderCharacter == null;
    }

    private static readonly int[] BoxFaces = {
        0,2,1, 1,2,3, 4,5,6, 5,7,6, 0,1,4, 1,5,4,
        2,6,3, 3,6,7, 0,4,2, 2,4,6, 1,3,5, 3,7,5
    };

    private static void Box(BoxCollider box, Action<Vector3, Vector3, Vector3> emit)
    {
        Vector3 half = box.size * 0.5f;
        var corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
            corners[i] = box.transform.TransformPoint(box.center + new Vector3((i & 1) == 0 ? -half.x : half.x,
                (i & 2) == 0 ? -half.y : half.y, (i & 4) == 0 ? -half.z : half.z));
        for (int i = 0; i < BoxFaces.Length; i += 3)
            emit(corners[BoxFaces[i]], corners[BoxFaces[i + 1]], corners[BoxFaces[i + 2]]);
    }

    private static void Capsule(Vector3 center, Vector3 axis, float radius, float segment, Action<Vector3, Vector3, Vector3> emit)
    {
        const int sides = 16, halfRings = 4;
        Vector3 right = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 forward = Vector3.Cross(axis, right).normalized;
        var previous = new Vector3[sides];
        for (int ring = 0; ring <= halfRings * 2 + 1; ring++)
        {
            bool upper = ring <= halfRings;
            float angle = upper ? ring * Mathf.PI / (halfRings * 2f) : (ring - 1) * Mathf.PI / (halfRings * 2f);
            float y = (upper ? segment : -segment) + Mathf.Cos(angle) * radius;
            float width = Mathf.Sin(angle) * radius;
            var current = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float azimuth = side * Mathf.PI * 2f / sides;
                current[side] = center + axis * y + (right * Mathf.Cos(azimuth) + forward * Mathf.Sin(azimuth)) * width;
            }
            if (ring > 0)
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    emit(previous[side], current[side], previous[next]);
                    emit(previous[next], current[side], current[next]);
                }
            previous = current;
        }
    }

    private MeshData GetMesh(Mesh mesh)
    {
        int id = mesh.GetInstanceID();
        if (meshes.TryGetValue(id, out MeshData data)) return data;
        data = new MeshData { Generation = generation }; meshes[id] = data;
        try
        {
            if (mesh.isReadable)
            {
                data.Vertices = mesh.vertices; data.Indices = mesh.triangles;
                readyMeshes++; Revision++; return data;
            }
            if (!SystemInfo.supportsAsyncGPUReadback || mesh.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32 ||
                mesh.GetVertexAttributeDimension(VertexAttribute.Position) < 3)
                throw new NotSupportedException("Mesh position format/readback is unsupported");
            int stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
            data.VertexCount = mesh.vertexCount;
            data.Stride = mesh.GetVertexBufferStride(stream);
            data.Offset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
            data.IndexFormat = mesh.indexFormat;
            data.Submeshes = new SubMeshDescriptor[mesh.subMeshCount];
            for (int i = 0; i < data.Submeshes.Length; i++) data.Submeshes[i] = mesh.GetSubMesh(i);
            if ((long)data.VertexCount * data.Stride > 64 * 1024 * 1024 || data.Stride <= 0)
                throw new NotSupportedException("Mesh readback exceeds local cache limit");
            GraphicsBuffer vertex = mesh.GetVertexBuffer(stream);
            AsyncGPUReadback.Request(vertex, request => {
                try { if (request.hasError) throw new InvalidOperationException("Vertex readback failed"); data.VertexBytes = request.GetData<byte>().ToArray(); Finish(data); }
                catch (Exception error) { Fail(data, error); }
                finally { vertex.Dispose(); }
            });
            GraphicsBuffer index = mesh.GetIndexBuffer();
            AsyncGPUReadback.Request(index, request => {
                try { if (request.hasError) throw new InvalidOperationException("Index readback failed"); data.IndexBytes = request.GetData<byte>().ToArray(); Finish(data); }
                catch (Exception error) { Fail(data, error); }
                finally { index.Dispose(); }
            });
        }
        catch (Exception error) { Fail(data, error); }
        return data;
    }

    private void Finish(MeshData data)
    {
        if (data.Generation != generation) { data.VertexBytes = data.IndexBytes = null; return; }
        if (data.Failed || data.Ready || data.VertexBytes == null || data.IndexBytes == null) return;
        var vertices = new Vector3[data.VertexCount];
        for (int i = 0; i < vertices.Length; i++)
        {
            int offset = i * data.Stride + data.Offset;
            vertices[i] = new Vector3(BitConverter.ToSingle(data.VertexBytes, offset), BitConverter.ToSingle(data.VertexBytes, offset + 4), BitConverter.ToSingle(data.VertexBytes, offset + 8));
        }
        var indices = new List<int>();
        int size = data.IndexFormat == IndexFormat.UInt16 ? 2 : 4;
        foreach (SubMeshDescriptor submesh in data.Submeshes)
        {
            if (submesh.topology != MeshTopology.Triangles) continue;
            for (int i = 0; i < submesh.indexCount; i++)
            {
                int offset = (submesh.indexStart + i) * size;
                int index = size == 2 ? BitConverter.ToUInt16(data.IndexBytes, offset) : checked((int)BitConverter.ToUInt32(data.IndexBytes, offset));
                indices.Add(index + submesh.baseVertex);
            }
        }
        data.Vertices = vertices; data.Indices = indices.ToArray();
        data.VertexBytes = data.IndexBytes = null;
        readyMeshes++; Revision++;
    }

    private void Fail(MeshData data, Exception error)
    {
        if (data.Generation != generation || data.Failed) return;
        data.Failed = true; data.VertexBytes = data.IndexBytes = null;
        failedMeshes++; Revision++;
        Plugin.Log.LogWarning("collision: mesh surface readback unavailable; sampled terrain remains active (" + error.GetType().Name + ")");
    }

    private void EmitMeshColumn(MeshCollider collider, MeshData data, int rx, int rz, float minX, float minZ,
        float bottom, float top, Action<Vector3, Vector3, Vector3> emit)
    {
        int id = collider.GetInstanceID();
        Matrix4x4 matrix = collider.transform.localToWorldMatrix;
        if (!worlds.TryGetValue(id, out WorldMesh world)) { world = new WorldMesh(); worlds[id] = world; }
        if (world.Mesh != collider.sharedMesh || world.Matrix != matrix || world.Vertices.Length != data.Vertices!.Length)
        {
            world.Mesh = collider.sharedMesh; world.Matrix = matrix; world.Columns.Clear();
            world.Vertices = new Vector3[data.Vertices!.Length];
            for (int i = 0; i < world.Vertices.Length; i++) world.Vertices[i] = matrix.MultiplyPoint3x4(data.Vertices[i]);
        }
        long key = ((long)rx << 32) | (uint)rz;
        if (!world.Columns.TryGetValue(key, out int[] offsets))
        {
            if (world.Columns.Count > 256) world.Columns.Clear();
            columnIndices.Clear();
            for (int i = 0; i + 2 < data.Indices!.Length; i += 3)
            {
                int a = data.Indices[i], b = data.Indices[i + 1], c = data.Indices[i + 2];
                if ((uint)a >= world.Vertices.Length || (uint)b >= world.Vertices.Length || (uint)c >= world.Vertices.Length) continue;
                Vector3 va = world.Vertices[a], vb = world.Vertices[b], vc = world.Vertices[c];
                if (Max3(va.x, vb.x, vc.x) < minX || Min3(va.x, vb.x, vc.x) > minX + 8f ||
                    Max3(va.z, vb.z, vc.z) < minZ || Min3(va.z, vb.z, vc.z) > minZ + 8f) continue;
                columnIndices.Add(i);
            }
            offsets = columnIndices.ToArray(); world.Columns[key] = offsets;
        }
        foreach (int i in offsets)
        {
            Vector3 a = world.Vertices[data.Indices![i]], b = world.Vertices[data.Indices[i + 1]], c = world.Vertices[data.Indices[i + 2]];
            if (Max3(a.y, b.y, c.y) >= bottom && Min3(a.y, b.y, c.y) <= top) emit(a, b, c);
        }
    }

    private static float Min3(float a, float b, float c) => Mathf.Min(a, Mathf.Min(b, c));
    private static float Max3(float a, float b, float c) => Mathf.Max(a, Mathf.Max(b, c));
}
