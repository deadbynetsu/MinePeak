using System;
using System.Buffers.Binary;

namespace PeakCraft.World;

/// <summary>Exact Minecraft collision boxes, separate from render triangles and imported PEAK terrain.</summary>
internal static class BlockCollisionPacket
{
    public const uint MessageType = 12;
    public const int HeaderBytes = 24, BoxBytes = 24, MaxBoxes = 65536;

    public static bool TryRead(ReadOnlySpan<byte> data, uint world, uint epoch,
        out int sx, out int sy, out int sz, out BlockCollisionBox[] boxes)
    {
        sx = sy = sz = 0;
        boxes = Array.Empty<BlockCollisionBox>();
        if (data.Length < HeaderBytes) return false;
        sx = Int(data, 0); sy = Int(data, 4); sz = Int(data, 8);
        int count = Int(data, 12);
        if (count < 0 || count > MaxBoxes || data.Length != HeaderBytes + count * BoxBytes ||
            unchecked((uint)Int(data, 16)) != world || unchecked((uint)Int(data, 20)) != epoch ||
            sx < -2000000 || sx > 2000000 || sz < -2000000 || sz > 2000000 || sy < -4096 || sy > 4096)
            return false;
        var parsed = new BlockCollisionBox[count];
        for (int i = 0; i < count; i++)
        {
            int offset = HeaderBytes + i * BoxBytes;
            var box = new BlockCollisionBox(Float(data, offset), Float(data, offset + 4), Float(data, offset + 8),
                Float(data, offset + 12), Float(data, offset + 16), Float(data, offset + 20));
            if (!box.Valid) return false;
            parsed[i] = box;
        }
        boxes = parsed;
        return true;
    }

    private static int Int(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
    private static float Float(ReadOnlySpan<byte> data, int offset) => BitConverter.Int32BitsToSingle(Int(data, offset));
}

internal readonly struct BlockCollisionBox
{
    public readonly float MinX, MinY, MinZ, MaxX, MaxY, MaxZ;
    public BlockCollisionBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    { MinX = minX; MinY = minY; MinZ = minZ; MaxX = maxX; MaxY = maxY; MaxZ = maxZ; }

    // Vanilla fences and moving-block shapes can extend outside their source block/section.
    public bool Valid => Axis(MinX, MaxX) && Axis(MinY, MaxY) && Axis(MinZ, MaxZ);
    private static bool Axis(float min, float max) => !float.IsNaN(min) && !float.IsNaN(max) &&
        !float.IsInfinity(min) && !float.IsInfinity(max) && min >= -2f && max <= 18f && min < max;
}
