using HarmonyLib;
using PeakCraft.Player;

namespace PeakCraft.Patches;

[HarmonyPatch(typeof(CharacterInput), "Sample")]
internal static class CharacterInputPatch
{
	private static void Postfix(CharacterInput __instance)
	{
		Plugin instance = Plugin.Instance;
		Character localCharacter = Character.localCharacter;
		if (!instance.Ownership.PeakInput && !(localCharacter == null) && !(localCharacter.input != __instance))
		{
			bool pauseWasPressed = __instance.pauseWasPressed;
			__instance.ResetInput();
			__instance.scrollInput = 0f;
			__instance.sprintToggleIsPressed = false;
			__instance.scrollBackwardWasPressed = false;
			__instance.scrollForwardWasPressed = false;
			__instance.scrollBackwardIsPressed = false;
			__instance.scrollForwardIsPressed = false;
			__instance.pingWasPressed = false;
			__instance.selectSlotForwardWasPressed = false;
			__instance.selectSlotBackwardWasPressed = false;
			__instance.unselectSlotWasPressed = false;
			__instance.selectBackpackWasPressed = false;
			__instance.spectateLeftWasPressed = false;
			__instance.spectateRightWasPressed = false;
			__instance.pushToTalkPressed = false;
			if (instance.Ownership.State == Owner.MinecraftOwns)
			{
				instance.Interact.Apply(__instance);
				__instance.pauseWasPressed = pauseWasPressed && !instance.InputBridge.ScreenOpen;
			}
			else
			{
				__instance.pauseWasPressed = false;
			}
		}
		if (instance.Ownership.State == Owner.NativeControl && localCharacter != null && localCharacter.input == __instance)
		{
			instance.Equipment.ApplyInput(__instance);
			instance.Climb.ApplyInput(__instance);
		}
		instance.Dev?.ApplyNativeInput(instance, __instance);
	}
}
