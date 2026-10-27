using System.Collections.Generic;
using UnityEngine;

namespace PeakCraft.Coop;

internal sealed class ScoutHider
{
	private readonly RemoteAvatars avatars;

	private readonly CoopNet net;

	private readonly Dictionary<Character, List<Renderer>> hidden = new Dictionary<Character, List<Renderer>>();

	private float nextScan;

	public ScoutHider(RemoteAvatars avatars, CoopNet net)
	{
		this.avatars = avatars;
		this.net = net;
	}

	public void Update()
	{
		if (Time.unscaledTime < nextScan)
		{
			return;
		}
		nextScan = Time.unscaledTime + 0.5f;
		HashSet<Character> hashSet = new HashSet<Character>();
		foreach (Character allCharacter in Character.AllCharacters)
		{
			if (allCharacter == null || allCharacter.IsLocal || allCharacter.view == null || allCharacter.view.Owner == null || !net.Share || !avatars.IsMinecraftPlayer(allCharacter.view.Owner.ActorNumber))
			{
				continue;
			}
			hashSet.Add(allCharacter);
			if (hidden.ContainsKey(allCharacter))
			{
				continue;
			}
			List<Renderer> list = new List<Renderer>();
			Renderer[] componentsInChildren = allCharacter.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				if (renderer.enabled)
				{
					renderer.enabled = false;
					list.Add(renderer);
				}
			}
			hidden[allCharacter] = list;
			Plugin.Log.LogInfo((object)$"coop: hid the scout of Minecraft player {allCharacter.view.Owner.ActorNumber} ({list.Count} renderers)");
		}
		List<Character> list2 = new List<Character>();
		foreach (KeyValuePair<Character, List<Renderer>> item in hidden)
		{
			if (item.Key == null || !hashSet.Contains(item.Key))
			{
				list2.Add(item.Key);
			}
		}
		foreach (Character item2 in list2)
		{
			foreach (Renderer item3 in hidden[item2])
			{
				if (item3 != null)
				{
					item3.enabled = true;
				}
			}
			hidden.Remove(item2);
		}
	}
}
