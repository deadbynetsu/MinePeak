using HarmonyLib;

namespace PeakCraft.Patches;

[HarmonyPatch(typeof(Character), "DieInstantly")]
internal static class CharacterPatch
{
	private static bool Prefix(Character __instance)
	{
		Plugin instance = Plugin.Instance;
		if (instance.Vitals?.Engaged == true) return true;
		if (instance.Death.Killing || !instance.Ownership.BodyFollows || __instance != Character.localCharacter)
		{
			return true;
		}
		instance.Hazards.Lethal();
		return false;
	}
}
