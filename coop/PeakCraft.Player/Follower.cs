using System.Collections.Generic;
using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;
using Zorro.Core;

namespace PeakCraft.Player;

internal sealed class Follower
{
	private readonly PeakLink link;

	private Character? held;

	private readonly List<Renderer> hidden = new List<Renderer>();

	private readonly List<Transform> roots = new List<Transform>();

	public Vector3 Feet { get; private set; }

	public float EyeHeight { get; private set; } = 1.62f;

	public Follower(PeakLink link)
	{
		this.link = link;
	}

	public static Vector3 PeakFeet(Character character)
	{
		return character.Center - Vector3.up * 1.089f;
	}

	public void Update(Ownership ownership, in Proto.GuestState guest)
	{
		Character localCharacter = Character.localCharacter;
		bool flag = ownership.BodyFollows && localCharacter != null;
		if (held != null && (!flag || held != localCharacter))
		{
			Release();
		}
		if (flag)
		{
			if (held == null)
			{
				Take(localCharacter);
			}
			Interpolate(in guest);
			Place(localCharacter, in guest);
		}
	}

	private void Take(Character character)
	{
		held = character;
		character.refs.climbing.StopAnyClimbing();
		character.refs.items.EquipSlot(Optionable<byte>.None);
		PeakApi.HaltBodyVelocity(character.refs.ragdoll);
		character.refs.ragdoll.ToggleKinematic(enableKinematic: true);
		roots.Clear();
		foreach (Bodypart part in character.refs.ragdoll.partList)
		{
			if (part.transform.parent == null || part.transform.parent.GetComponentInParent<Bodypart>() == null)
			{
				roots.Add(part.transform);
			}
		}
		hidden.Clear();
		Renderer[] componentsInChildren = character.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer.enabled)
			{
				renderer.enabled = false;
				hidden.Add(renderer);
			}
		}
		Plugin.Log.LogInfo((object)$"follower: took PEAK's character ({hidden.Count} renderers hidden, ragdoll kinematic, {roots.Count} root bodypart(s))");
	}

	public void Release()
	{
		if (held == null)
		{
			return;
		}
		foreach (Renderer item in hidden)
		{
			if (item != null)
			{
				item.enabled = true;
			}
		}
		hidden.Clear();
		if (held != null)
		{
			held.refs.ragdoll.ToggleKinematic(enableKinematic: false);
			PeakApi.HaltBodyVelocity(held.refs.ragdoll);
			held.data.sinceGrounded = 0f;
			held.input.itemSwitchBlocked = false;
		}
		held = null;
		Plugin.Log.LogInfo((object)"follower: released PEAK's character");
	}

	private void Interpolate(in Proto.GuestState guest)
	{
		Vector3 vector = Coords.ToUnity(guest.x, guest.y, guest.z);
		float eyeHeight = ((guest.eyeHeight > 0.1f) ? guest.eyeHeight : 1.62f);
		if (guest.tickQpc != 0L && guest.tickMs > 1f)
		{
			double num = (double)(PeakLink.Qpc() - guest.tickQpc) * 1000.0 / (double)link.QpcFrequency;
			if (num >= 0.0 && num < 250.0)
			{
				float t = Mathf.Clamp01((float)(num / (double)guest.tickMs));
				Vector3 a = Coords.ToUnity(guest.prevX, guest.prevY, guest.prevZ);
				Vector3 b = Coords.ToUnity(guest.curX, guest.curY, guest.curZ);
				Vector3 vector2 = Vector3.Lerp(a, b, t);
				if ((vector2 - vector).sqrMagnitude < 4f)
				{
					vector = vector2;
					eyeHeight = Mathf.Lerp(guest.tickEyeO, guest.tickEye, t);
				}
			}
		}
		Feet = vector;
		EyeHeight = eyeHeight;
	}

	private void Place(Character character, in Proto.GuestState guest)
	{
		Vector3 vector = Feet - PeakFeet(character);
		if (vector.sqrMagnitude > 1E-10f && vector.sqrMagnitude < 1E+12f)
		{
			foreach (Transform root in roots)
			{
				root.position += vector;
			}
		}
		CharacterData data = character.data;
		data.isGrounded = true;
		data.sinceGrounded = 0f;
		data.fallSeconds = 0f;
		data.isSprinting = false;
		data.isCrouching = false;
		character.input.itemSwitchBlocked = true;
	}
}
