using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using PeakCraft.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeakCraft.Dev;

internal sealed class MeltyTest
{
	private const string Slug = "peakcraft-co-op";

	private const float ControlSeconds = 10f;

	private readonly string checksPath;

	private readonly HashSet<string> reported = new HashSet<string>();

	private bool startedSolo;

	private float ownedSince = -1f;

	private MeltyTest(string checksPath)
	{
		this.checksPath = checksPath;
	}

	public static MeltyTest? Detect()
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Melty", "test", "peakcraft-co-op.json");
			if (!File.Exists(path))
			{
				return null;
			}
			string text = (string?)JObject.Parse(File.ReadAllText(path))["checks"];
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			Plugin.Log.LogInfo((object)("melty test: on, checks go to " + text));
			return new MeltyTest(text);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("melty test: couldn't read the test file: " + ex.Message));
			return null;
		}
	}

	public void Pass(string name)
	{
		Report(name, null);
	}

	public void Fail(string name, string why)
	{
		Report(name, why);
	}

	private void Report(string name, string? failure)
	{
		if (reported.Add(name))
		{
			string text = ((failure == null) ? ("pass " + name) : ("fail " + name + ": " + failure));
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(checksPath));
				File.AppendAllText(checksPath, text + "\n");
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning((object)("melty test: couldn't write '" + text + "': " + ex.Message));
			}
			Plugin.Log.LogInfo((object)("melty test: " + text));
		}
	}

	public void Update(bool linked, Owner owner)
	{
		StartSolo();
		if (linked)
		{
			Pass("minecraft-linked");
		}
		if (!linked || owner != Owner.MinecraftOwns || !(Character.localCharacter != null))
		{
			ownedSince = -1f;
		}
		else if (ownedSince < 0f)
		{
			ownedSince = Time.unscaledTime;
		}
		else if (Time.unscaledTime - ownedSince > 10f)
		{
			Pass("minecraft-controls-climber");
		}
	}

	private void StartSolo()
	{
		if (!startedSolo && !LoadingScreenHandler.loading && !(SceneManager.GetActiveScene().name != "Title"))
		{
			MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenu>();
			if (!(mainMenu == null) && !(Time.timeSinceLevelLoad < 3f))
			{
				startedSolo = true;
				Plugin.Log.LogInfo((object)"melty test: starting a solo run from the title screen");
				mainMenu.PlaySoloClicked();
			}
		}
	}
}
