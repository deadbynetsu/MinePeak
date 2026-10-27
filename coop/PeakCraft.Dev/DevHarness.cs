using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PeakCraft.Dev;

internal sealed class DevHarness
{
	private readonly Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);

	private readonly string commandFile = Path.Combine(Paths.BepInExRootPath, "peakcraft-dev.txt");

	private readonly string shotDir = Path.Combine(Paths.BepInExRootPath, "peakcraft-shots");

	private float nextPoll;

	private bool startedOffline;

	public DevHarness()
	{
		Directory.CreateDirectory(shotDir);
		Register("shot", (string[] a) =>
		{
			Shot((a.Length != 0) ? a[0] : "shot");
		});
		Register("probe", (string[] _) =>
		{
			Probe();
		});
		Register("log", (string[] a) =>
		{
			Plugin.Log.LogInfo((object)("dev: " + string.Join(" ", a)));
		});
		Register("quit", (string[] _) =>
		{
			Application.Quit();
		});
		Plugin.Log.LogInfo((object)("dev harness on: commands from " + commandFile));
	}

	public void Register(string name, Action<string[]> action)
	{
		commands[name] = action;
	}

	public static float F(string s)
	{
		return float.Parse(s, CultureInfo.InvariantCulture);
	}

	public void Update()
	{
		AutoStartOffline();
		if (Time.unscaledTime < nextPoll)
		{
			return;
		}
		nextPoll = Time.unscaledTime + 0.1f;
		if (!File.Exists(commandFile))
		{
			return;
		}
		string[] array;
		try
		{
			array = File.ReadAllLines(commandFile);
			File.Delete(commandFile);
		}
		catch (IOException)
		{
			return;
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			string[] array3 = text.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (array3.Length == 0)
			{
				continue;
			}
			if (!commands.TryGetValue(array3[0], out Action<string[]> value))
			{
				Plugin.Log.LogWarning((object)("dev: unknown command '" + array3[0] + "'"));
				continue;
			}
			try
			{
				value(array3.Skip(1).ToArray());
			}
			catch (Exception arg)
			{
				Plugin.Log.LogError((object)$"dev: '{text}' failed: {arg}");
			}
		}
	}

	private void AutoStartOffline()
	{
		if (!startedOffline && !LoadingScreenHandler.loading && !(SceneManager.GetActiveScene().name != "Title"))
		{
			MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenu>();
			if (!(mainMenu == null) && !(Time.timeSinceLevelLoad < 3f))
			{
				startedOffline = true;
				Plugin.Log.LogInfo((object)"dev: starting an offline session from the title screen");
				mainMenu.PlaySoloClicked();
			}
		}
	}

	private void Shot(string name)
	{
		string text = Path.Combine(shotDir, name + ".png");
		ScreenCapture.CaptureScreenshot(text);
		Plugin.Log.LogInfo((object)("dev: screenshot -> " + text));
	}

	private static void Probe()
	{
		ManualLogSource log = Plugin.Log;
		Scene activeScene = SceneManager.GetActiveScene();
		log.LogInfo((object)$"probe: scene '{activeScene.name}', screen {Screen.width}x{Screen.height}, graphics {SystemInfo.graphicsDeviceType}");
		StringBuilder stringBuilder = new StringBuilder("probe: bindings");
		foreach (InputAction action in InputSystem.actions)
		{
			stringBuilder.Append("\n  ").Append(action.actionMap?.name).Append('/')
				.Append(action.name)
				.Append(": ")
				.Append(string.Join(", ", from b in action.bindings
					where !b.isComposite
					select b.effectivePath));
		}
		log.LogInfo((object)stringBuilder.ToString());
		Character localCharacter = Character.localCharacter;
		if (localCharacter != null)
		{
			float num = float.MaxValue;
			float num2 = float.MinValue;
			foreach (Collider collider2 in localCharacter.refs.ragdoll.colliderList)
			{
				num = Mathf.Min(num, collider2.bounds.min.y);
				num2 = Mathf.Max(num2, collider2.bounds.max.y);
			}
			Physics.Raycast(localCharacter.Center, Vector3.down, out var hitInfo, 10f, LayerMask.GetMask("Terrain", "Map", "Default"), QueryTriggerInteraction.Ignore);
			log.LogInfo((object)$"probe: character center {localCharacter.Center:F3} head {localCharacter.Head:F3} hip {localCharacter.HipPos():F3} collider y {num:F3}..{num2:F3} (height {num2 - num:F3}) ground y {hitInfo.point.y:F3} camera {MainCamera.instance.transform.position:F3} fov {MainCamera.instance.cam.fieldOfView:F1}");
			log.LogInfo((object)$"probe: lookValues {localCharacter.data.lookValues} lookDirection {localCharacter.data.lookDirection:F3} grounded {localCharacter.data.isGrounded}");
		}
		int mask = LayerMask.GetMask("Terrain", "Map", "Default");
		Bounds bounds = default;
		bool flag = false;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Collider[] array = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
		foreach (Collider collider in array)
		{
			string key = LayerMask.LayerToName(collider.gameObject.layer);
			dictionary[key] = ((!dictionary.TryGetValue(key, out var value)) ? 1 : (value + 1));
			if (collider.isTrigger || !collider.enabled || (mask & (1 << collider.gameObject.layer)) == 0)
			{
				continue;
			}
			num3++;
			if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
			{
				num4++;
				if (meshCollider.sharedMesh.isReadable)
				{
					num5++;
				}
			}
			if (collider is TerrainCollider)
			{
				num6++;
			}
			if (!flag)
			{
				bounds = collider.bounds;
				flag = true;
			}
			else
			{
				bounds.Encapsulate(collider.bounds);
			}
		}
		log.LogInfo((object)$"probe: {num3} solid colliders on Terrain/Map/Default, world bounds min {bounds.min:F1} max {bounds.max:F1}; {num4} mesh colliders ({num5} readable), {num6} terrain colliders");
		log.LogInfo((object)("probe: colliders per layer: " + string.Join(", ", from p in dictionary
			orderby p.Value descending
			select $"{p.Key}={p.Value}")));
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		Renderer[] array2 = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
		for (int num7 = 0; num7 < array2.Length; num7++)
		{
			Material[] sharedMaterials = array2[num7].sharedMaterials;
			foreach (Material material in sharedMaterials)
			{
				if (material != null && material.shader != null)
				{
					dictionary2[material.shader.name] = ((!dictionary2.TryGetValue(material.shader.name, out var value2)) ? 1 : (value2 + 1));
				}
			}
		}
		log.LogInfo((object)("probe: shaders on scene renderers: " + string.Join(", ", from p in dictionary2
			orderby p.Value descending
			select $"{p.Key}={p.Value}")));
		stringBuilder = new StringBuilder("probe: loaded shaders");
		foreach (Shader item in from s in Resources.FindObjectsOfTypeAll<Shader>()
			orderby s.name
			select s)
		{
			List<string> list = new List<string>();
			for (int num9 = 0; num9 < item.GetPropertyCount(); num9++)
			{
				list.Add(item.GetPropertyName(num9));
			}
			bool flag2 = list.Any((string p) =>
			{
				switch (p)
				{
				case "_Cutoff":
				case "_AlphaClip":
				case "_SrcBlend":
				case "_BaseMap":
				case "_MainTex":
					return true;
				default:
					return false;
				}
			});
			stringBuilder.Append("\n  ").Append(item.name).Append(item.isSupported ? "" : " (unsupported)");
			if (flag2)
			{
				stringBuilder.Append(" [").Append(string.Join(" ", list.Take(40))).Append(']');
			}
		}
		log.LogInfo((object)stringBuilder.ToString());
	}
}
