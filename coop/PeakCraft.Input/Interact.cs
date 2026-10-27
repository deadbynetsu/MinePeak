using UnityEngine;

namespace PeakCraft.Input;

internal sealed class Interact
{
	private bool held;

	private float devHoldUntil = -1f;

	public bool Held => held;

	public bool Pressed { get; private set; }

	public bool Released { get; private set; }

	public void DevHold(float seconds)
	{
		devHoldUntil = Time.unscaledTime + seconds;
	}

	public void Update(bool keyDown)
	{
		bool flag = keyDown || Time.unscaledTime < devHoldUntil;
		Pressed = flag && !held;
		Released = !flag && held;
		held = flag;
		if (Pressed)
		{
			IInteractible interactible = ((Interaction.instance != null) ? Interaction.instance.bestInteractable : null);
			Plugin.Log.LogInfo((object)((interactible != null) ? ("interact: PEAK target '" + interactible.GetTransform().name + "' (" + interactible.GetInteractionText() + ")") : "interact: nothing targeted"));
		}
	}

	public void Apply(CharacterInput input)
	{
		input.interactWasPressed = Pressed;
		input.interactIsPressed = held;
		input.interactWasReleased = Released;
	}
}
