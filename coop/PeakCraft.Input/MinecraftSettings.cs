using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;

namespace PeakCraft.Input;

internal static class MinecraftSettings
{
	public static void Open(Plugin plugin)
	{
		GUIManager instance = GUIManager.instance;
		if (!(instance == null) && PeakApi.InPauseMenu(instance) && !(instance.pauseMenuMainPage == null) && instance.pauseMenuMainPage.isActiveAndEnabled && !plugin.CoopMode.IsViewer && plugin.Link.GuestAlive)
		{
			instance.pauseMenuMainPage.OnResumeClicked();
			instance.UpdateWindowStatus();
			plugin.Link.PushInput(Proto.InputType.ReleaseAll, 0);
			plugin.Link.PushInput((Proto.InputType)10, 0);
			Plugin.Log.LogInfo((object)"settings: opening Minecraft options from PEAK pause menu");
		}
	}
}
