using BepInEx.Configuration;
using UnityEngine;

namespace PeakCraft.Coop;

internal sealed class CoopConfig
{
	public ConfigEntry<string> CoopMode { get; }

	public ConfigEntry<KeyCode> CoopToggleKey { get; }

	public ConfigEntry<bool> CoopShare { get; }

	public ConfigEntry<string> CoopMcJar { get; }

	public ConfigEntry<bool> CoopLoopback { get; }

	public CoopConfig(ConfigFile config)
	{
		CoopMode = config.Bind<string>("Coop", "Mode", "Viewer", "Minecraft or Viewer/native PEAK. Switch live in the pause menu or with F8; no restart needed.");
		CoopToggleKey = config.Bind<KeyCode>("Coop", "ToggleModeKey", KeyCode.F8, "Switches live between Minecraft and native PEAK. No restart needed.");
		CoopShare = config.Bind<bool>("Coop", "Share", true, "Share your skin, held item, position and blocks with other PeakCraft players in your lobby, and show theirs.");
		CoopMcJar = config.Bind<string>("Coop", "MinecraftJar", "auto", "Your Minecraft client jar, for drawing other players' blocks and items in viewer mode. auto finds it (PeakCraft's Prism download of 26.3, then .minecraft/versions).");
		CoopLoopback = config.Bind<bool>("Dev", "CoopLoopback", false, "Development only: your own co-op messages come back as another player 3 blocks east, to test sharing alone.");
	}
}
