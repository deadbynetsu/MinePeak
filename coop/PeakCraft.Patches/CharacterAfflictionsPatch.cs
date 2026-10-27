using HarmonyLib;

namespace PeakCraft.Patches;

[HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
internal static class CharacterAfflictionsPatch
{
	private static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, float amount, ref bool __result)
	{
		if (!Plugin.Instance.Hazards.Intercept(__instance, statusType, amount))
		{
			return true;
		}
		__result = false;
		return false;
	}
}
