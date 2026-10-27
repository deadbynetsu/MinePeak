using BepInEx.Configuration;
using PeakCraft.Link;
using PeakCraft.Player;
using UnityEngine;

namespace PeakCraft.Hazards;

internal sealed class AfflictionBridge
{
	private const float SendInterval = 0.5f;

	private const float HealthPerFullBar = 20f;

	private const float WireScale = 500f;

	private readonly PeakLink link;

	private readonly ConfigEntry<float> scale;

	private float pending;

	private float lastSend;

	private float lastLog;

	private float loggedDamage;

	public AfflictionBridge(PeakLink link, ConfigFile config)
	{
		this.link = link;
		scale = config.Bind<float>("Hazards", "DamageScale", 3f, "Multiplies the Minecraft damage PEAK's hazards deal in Survival. At 1, filling PEAK's whole status bar costs 10 hearts, which Minecraft's natural healing outruns; 3 makes the fog kill in under a minute.");
	}

	private static bool Hurts(CharacterAfflictions.STATUSTYPE type)
	{
		switch (type)
		{
		case CharacterAfflictions.STATUSTYPE.Injury:
		case CharacterAfflictions.STATUSTYPE.Cold:
		case CharacterAfflictions.STATUSTYPE.Poison:
		case CharacterAfflictions.STATUSTYPE.Drowsy:
		case CharacterAfflictions.STATUSTYPE.Hot:
		case CharacterAfflictions.STATUSTYPE.Thorns:
		case CharacterAfflictions.STATUSTYPE.Spores:
			return true;
		default:
			return false;
		}
	}

	public bool Intercept(CharacterAfflictions afflictions, CharacterAfflictions.STATUSTYPE type, float amount)
	{
		if (Plugin.Instance.Vitals?.Engaged == true) return false;
		if (!Plugin.Instance.Ownership.BodyFollows || afflictions.character != Character.localCharacter)
		{
			return false;
		}
		if (amount > 0f && Hurts(type))
		{
			pending += amount;
		}
		return true;
	}

	public void Lethal()
	{
		pending += 100f;
	}

	public void Update(Ownership ownership)
	{
		if (Plugin.Instance.Vitals?.Engaged == true) { pending = 0f; return; }
		Character localCharacter = Character.localCharacter;
		if (!ownership.BodyFollows || localCharacter == null)
		{
			pending = 0f;
			return;
		}
		float[] currentStatuses = localCharacter.refs.afflictions.currentStatuses;
		for (int i = 0; i < currentStatuses.Length; i++)
		{
			currentStatuses[i] = 0f;
		}
		if (!(pending <= 0f) && !(Time.unscaledTime - lastSend < 0.5f))
		{
			float num = pending * 20f * scale.Value;
			pending = 0f;
			lastSend = Time.unscaledTime;
			link.PushInput(Proto.InputType.Hurt, 3, Mathf.Min((int)(num * 500f), 1000000));
			loggedDamage += num;
			if (Time.unscaledTime - lastLog > 5f)
			{
				Plugin.Log.LogInfo((object)$"hazards: {loggedDamage:F2} Minecraft damage sent in the last {Time.unscaledTime - lastLog:F0} s (Creative ignores it)");
				lastLog = Time.unscaledTime;
				loggedDamage = 0f;
			}
		}
	}
}
