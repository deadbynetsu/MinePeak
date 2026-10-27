using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;

namespace PeakCraft.Hazards;

internal sealed class Death
{
	private readonly PeakLink link;

	private float deadSince = -1f;

	private float nextClick;

	private bool buttonDown;

	public bool Killing { get; private set; }

	public Death(PeakLink link)
	{
		this.link = link;
	}

	public void OnMinecraftDeath()
	{
		Character localCharacter = Character.localCharacter;
		if (localCharacter == null || PeakApi.IsDead(localCharacter.data))
		{
			return;
		}
		if (localCharacter.inAirport)
		{
			Plugin.Log.LogInfo((object)"death: Minecraft player died in the airport; PEAK has no death there, Minecraft just respawns");
			return;
		}
		Plugin.Log.LogInfo((object)"death: Minecraft player died; running PEAK's death for the scout");
		Killing = true;
		try
		{
			localCharacter.DieInstantly();
		}
		finally
		{
			Killing = false;
		}
	}

	public void Update(bool linked, in Proto.GuestState guest)
	{
		if (!linked || (guest.flags & 0x20) == 0)
		{
			deadSince = -1f;
			if (buttonDown)
			{
				buttonDown = false;
				link.PushInput(Proto.InputType.MouseButton, 1);
			}
			return;
		}
		if (deadSince < 0f)
		{
			deadSince = Time.unscaledTime;
			nextClick = deadSince + 2f;
		}
		if (buttonDown)
		{
			buttonDown = false;
			link.PushInput(Proto.InputType.MouseButton, 1);
		}
		else if (!(Time.unscaledTime < nextClick) && (guest.flags & 2) != 0)
		{
			nextClick = Time.unscaledTime + 1f;
			int num = Mathf.Max(1, (int)guest.guiScale);
			int num2 = Screen.width / 2;
			int num3 = (Screen.height / num / 4 + 82) * num;
			link.PushInput(Proto.InputType.Cursor, 0, num2, num3);
			link.PushInput(Proto.InputType.MouseButton, 1, 1);
			buttonDown = true;
			Plugin.Log.LogInfo((object)$"death: clicking Respawn on Minecraft's death screen at ({num2}, {num3})");
		}
	}
}
