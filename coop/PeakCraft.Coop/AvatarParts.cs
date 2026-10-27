using UnityEngine;

namespace PeakCraft.Coop;

internal static class AvatarParts
{
	public static readonly AvatarPart[] All = new AvatarPart[12]
	{
		new AvatarPart("head", new Vector3(0f, 24f, 0f), new Vector3(-4f, 24f, -4f), new Vector3(8f, 8f, 8f), new Vector2Int(0, 0), 0f, new Vector3(8f, 8f, 8f), new Vector3(-4f, 24f, -4f), Swing.Head, overlay: false),
		new AvatarPart("hat", new Vector3(0f, 24f, 0f), new Vector3(-4f, 24f, -4f), new Vector3(8f, 8f, 8f), new Vector2Int(32, 0), 0.5f, new Vector3(8f, 8f, 8f), new Vector3(-4f, 24f, -4f), Swing.Head, overlay: true),
		new AvatarPart("body", new Vector3(0f, 24f, 0f), new Vector3(-4f, 12f, -2f), new Vector3(8f, 12f, 4f), new Vector2Int(16, 16), 0f, new Vector3(8f, 12f, 4f), new Vector3(-4f, 12f, -2f), Swing.None, overlay: false),
		new AvatarPart("jacket", new Vector3(0f, 24f, 0f), new Vector3(-4f, 12f, -2f), new Vector3(8f, 12f, 4f), new Vector2Int(16, 32), 0.25f, new Vector3(8f, 12f, 4f), new Vector3(-4f, 12f, -2f), Swing.None, overlay: true),
		new AvatarPart("right_arm", new Vector3(5f, 22f, 0f), new Vector3(4f, 12f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(40, 16), 0f, new Vector3(3f, 12f, 4f), new Vector3(4f, 12f, -2f), Swing.RightArm, overlay: false),
		new AvatarPart("right_sleeve", new Vector3(5f, 22f, 0f), new Vector3(4f, 12f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(40, 32), 0.25f, new Vector3(3f, 12f, 4f), new Vector3(4f, 12f, -2f), Swing.RightArm, overlay: true),
		new AvatarPart("left_arm", new Vector3(-5f, 22f, 0f), new Vector3(-8f, 12f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(32, 48), 0f, new Vector3(3f, 12f, 4f), new Vector3(-7f, 12f, -2f), Swing.LeftArm, overlay: false),
		new AvatarPart("left_sleeve", new Vector3(-5f, 22f, 0f), new Vector3(-8f, 12f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(48, 48), 0.25f, new Vector3(3f, 12f, 4f), new Vector3(-7f, 12f, -2f), Swing.LeftArm, overlay: true),
		new AvatarPart("right_leg", new Vector3(2f, 12f, 0f), new Vector3(0f, 0f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(0, 16), 0f, new Vector3(4f, 12f, 4f), new Vector3(0f, 0f, -2f), Swing.RightLeg, overlay: false),
		new AvatarPart("right_pants", new Vector3(2f, 12f, 0f), new Vector3(0f, 0f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(0, 32), 0.25f, new Vector3(4f, 12f, 4f), new Vector3(0f, 0f, -2f), Swing.RightLeg, overlay: true),
		new AvatarPart("left_leg", new Vector3(-2f, 12f, 0f), new Vector3(-4f, 0f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(16, 48), 0f, new Vector3(4f, 12f, 4f), new Vector3(-4f, 0f, -2f), Swing.LeftLeg, overlay: false),
		new AvatarPart("left_pants", new Vector3(-2f, 12f, 0f), new Vector3(-4f, 0f, -2f), new Vector3(4f, 12f, 4f), new Vector2Int(0, 48), 0.25f, new Vector3(4f, 12f, 4f), new Vector3(-4f, 0f, -2f), Swing.LeftLeg, overlay: true)
	};
}
