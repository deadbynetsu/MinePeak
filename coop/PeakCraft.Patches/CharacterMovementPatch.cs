using HarmonyLib;

namespace PeakCraft.Patches;

[HarmonyPatch(typeof(CharacterMovement))]
internal static class CharacterMovementPatch
{
	[HarmonyPatch("FixedUpdate")]
	[HarmonyPrefix]
	private static bool FixedUpdatePrefix(CharacterMovement __instance)
	{
		if (Plugin.Instance.Ownership.BodyFollows)
		{
			return !IsLocal(__instance);
		}
		return true;
	}

	[HarmonyPatch("CameraLook")]
	[HarmonyPrefix]
	private static bool CameraLookPrefix(CharacterMovement __instance)
	{
		Plugin instance = Plugin.Instance;
		if (!instance.Ownership.BodyFollows || !IsLocal(__instance))
		{
			return true;
		}
		instance.Camera.ApplyLookToPeak(__instance.character);
		return false;
	}

	private static bool IsLocal(CharacterMovement movement)
	{
		Character localCharacter = Character.localCharacter;
		if (localCharacter != null)
		{
			return movement.character == localCharacter;
		}
		return false;
	}
}
