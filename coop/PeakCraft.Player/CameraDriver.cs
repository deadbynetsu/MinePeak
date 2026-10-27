using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;

namespace PeakCraft.Player;

internal sealed class CameraDriver
{
	private float lastCompare;

	public float Yaw;

	public float Pitch;

	public void SetLookFromPeak(Character character)
	{
		Yaw = Coords.McYawFromUnity(character.data.lookValues.x);
		Pitch = Mathf.Clamp(0f - character.data.lookValues.y, -90f, 90f);
	}

	public void ApplyLookToPeak(Character character)
	{
		character.data.lookValues = new Vector2(Coords.UnityYawFromMc(Yaw), Mathf.Clamp(0f - Pitch, -85f, 85f));
		character.RecalculateLookDirections();
	}

	public void Apply(Follower follower, in Proto.GuestState guest)
	{
		MainCamera instance = MainCamera.instance;
		if (!(instance == null) && !(instance.cam == null))
		{
			Vector3 vector = follower.Feet + Vector3.up * follower.EyeHeight;
			Quaternion quaternion = Coords.Rotation(Yaw, Pitch);
			Vector3 position = vector;
			if (guest.cameraMode == 1)
			{
				position = vector - quaternion * Vector3.forward * guest.cameraDistance;
			}
			else if (guest.cameraMode == 2)
			{
				position = vector + quaternion * Vector3.forward * guest.cameraDistance;
				quaternion = Coords.Rotation(Yaw + 180f, 0f - Pitch);
			}
			instance.transform.SetPositionAndRotation(position, quaternion);
			if (guest.fovDeg > 1f)
			{
				instance.cam.fieldOfView = guest.fovDeg;
			}
			if (Time.unscaledTime - lastCompare > 5f)
			{
				lastCompare = Time.unscaledTime;
				Vector3 vector2 = Coords.ToUnity(guest.eyeX, guest.eyeY, guest.eyeZ);
				Plugin.Log.LogInfo((object)$"camera: PEAK camera {instance.transform.position:F3}, Minecraft eye {vector2:F3} (Unity space), apart {(vector - vector2).magnitude * 100f:F1} cm, mode {guest.cameraMode}, fov {guest.fovDeg:F1}");
			}
		}
	}
}
