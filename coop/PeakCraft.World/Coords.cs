using UnityEngine;

namespace PeakCraft.World;

internal static class Coords
{
	public static double YOffset = -512.0;

	public const float CenterAboveFeet = 1.089f;

	public static void ToMc(Vector3 unity, out double x, out double y, out double z)
	{
		x = unity.x;
		y = (double)unity.y + YOffset;
		z = 0f - unity.z;
	}

	public static Vector3 ToUnity(double x, double y, double z)
	{
		return new Vector3((float)x, (float)(y - YOffset), (float)(0.0 - z));
	}

	public static float McYawFromUnity(float unityYaw)
	{
		return Mathf.DeltaAngle(0f, unityYaw + 180f);
	}

	public static float UnityYawFromMc(float mcYaw)
	{
		return Mathf.DeltaAngle(0f, mcYaw - 180f);
	}

	public static Quaternion Rotation(float mcYaw, float mcPitch)
	{
		return Quaternion.Euler(mcPitch, mcYaw + 180f, 0f);
	}
}
