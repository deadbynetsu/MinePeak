using UnityEngine;

namespace PeakCraft.Coop;

internal readonly struct AvatarPart(string id, Vector3 pivot, Vector3 from, Vector3 size, Vector2Int uv, float inflate, Vector3 slimSize, Vector3 slimFrom, Swing swing, bool overlay)
{
	public readonly string Id = id;

	public readonly Vector3 Pivot = pivot;

	public readonly Vector3 From = from;

	public readonly Vector3 Size = size;

	public readonly Vector3 SlimFrom = slimFrom;

	public readonly Vector3 SlimSize = slimSize;

	public readonly Vector2Int Uv = uv;

	public readonly float Inflate = inflate;

	public readonly Swing Swing = swing;

	public readonly bool Overlay = overlay;
}
