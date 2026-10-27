using HarmonyLib;

namespace PeakCraft.Patches;

[HarmonyPatch(typeof(MainCameraMovement), "LateUpdate")]
internal static class MainCameraMovementPatch
{
	private static void Postfix()
	{
		Plugin instance = Plugin.Instance;
		if (!instance.Ownership.PeakCamera)
		{
			instance.Camera.Apply(instance.Follower, instance.Guest);
		}
	}
}
