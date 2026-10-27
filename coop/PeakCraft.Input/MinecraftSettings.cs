using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;

namespace PeakCraft.Input;

internal static class MinecraftSettings
{
	private static GUIStyle? buttonStyle;

	public static void Draw(Plugin plugin)
	{
		GUIManager instance = GUIManager.instance;
		if (!(instance == null) && PeakApi.InPauseMenu(instance) && !(instance.pauseMenuMainPage == null) && instance.pauseMenuMainPage.isActiveAndEnabled && !plugin.CoopMode.IsViewer && plugin.Link.GuestAlive)
		{
			if (buttonStyle == null)
			{
				buttonStyle = new GUIStyle(GUI.skin.button)
				{
					fontStyle = FontStyle.Bold
				};
			}
			float num = Mathf.Clamp((float)Screen.height / 1080f, 0.6f, 2f);
			buttonStyle.fontSize = Mathf.RoundToInt(22f * num);
			if (GUI.Button(new Rect((float)Screen.width * 0.5f - 190f * num, (float)Screen.height - 100f * num, 380f * num, 54f * num), "Minecraft Settings", buttonStyle))
			{
				instance.pauseMenuMainPage.OnResumeClicked();
				instance.UpdateWindowStatus();
				plugin.Link.PushInput(Proto.InputType.ReleaseAll, 0);
				plugin.Link.PushInput((Proto.InputType)10, 0);
				Plugin.Log.LogInfo((object)"settings: opening Minecraft options from PEAK pause menu");
			}
		}
	}
}
