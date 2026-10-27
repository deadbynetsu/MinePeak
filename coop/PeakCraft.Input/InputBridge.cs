using System.Collections.Generic;
using BepInEx.Configuration;
using PeakCraft.Link;
using PeakCraft.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace PeakCraft.Input;

internal sealed class InputBridge
{
	private readonly PeakLink link;

	private readonly CameraDriver camera;

	private readonly Interact interact;

	private readonly ConfigEntry<Key> interactKey;

	private readonly ConfigEntry<Key> menuKey;

	private readonly HashSet<Key> held = new HashSet<Key>();

	private readonly List<Key> scratch = new List<Key>();

	private readonly bool[] buttons = new bool[6];

	private readonly Queue<char> text = new Queue<char>();

	private Keyboard? textSource;

	private bool routing;
	private bool waitForMouseRelease;

	private bool screenOpen;

	private float cursorX;

	private float cursorY;

	private float sensitivity = 0.5f;

	private Key repeatKey;

	private float repeatAt;

	public bool ScreenOpen
	{
		get
		{
			if (routing)
			{
				return screenOpen;
			}
			return false;
		}
	}

	public Vector2 Cursor => new Vector2(cursorX, cursorY);

	public string InteractKeyName => interactKey.Value.ToString();

	public InputBridge(PeakLink link, CameraDriver camera, Interact interact, ConfigFile config)
	{
		this.link = link;
		this.camera = camera;
		this.interact = interact;
		interactKey = config.Bind<Key>("Keys", "PeakInteract", Key.G, "Triggers PEAK's interact on whatever PEAK targets (kiosk, campfire, luggage). Hold it where PEAK wants a hold.");
		menuKey = config.Bind<Key>("Keys", "MinecraftMenu", Key.O, "Opens Minecraft's pause menu. Esc opens PEAK's menu, or closes an open Minecraft screen first.");
	}

	public void Update(Ownership ownership, in Proto.GuestState guest)
	{
		Keyboard current = Keyboard.current;
		Mouse current2 = Mouse.current;
		bool flag = ownership.State == Owner.MinecraftOwns && Application.isFocused && current != null && current2 != null;
		if (flag != routing)
		{
			routing = flag;
			ReleaseAll();
			if (flag) waitForMouseRelease = true;
			Plugin.Log.LogInfo((object)(flag ? "input: keyboard and mouse go to Minecraft" : "input: released (release-all sent)"));
		}
		if (!flag)
		{
			interact.Update(ownership.State == Owner.NativeControl && Application.isFocused && current != null && current[interactKey.Value].isPressed);
			return;
		}
		if (guest.sensitivity >= 0f && guest.sensitivity <= 1f)
		{
			sensitivity = guest.sensitivity;
		}
		HookText(current);
		bool flag2 = (guest.flags & 2) != 0;
		if (flag2 && !screenOpen)
		{
			cursorX = (float)Screen.width * 0.5f;
			cursorY = (float)Screen.height * 0.5f;
			link.PushInput(Proto.InputType.Cursor, 0, (int)cursorX, (int)cursorY);
		}
		screenOpen = flag2;
		Vector2 vector = current2.delta.ReadValue();
		if (screenOpen)
		{
			if (vector != Vector2.zero)
			{
				cursorX = Mathf.Clamp(cursorX + vector.x, 0f, Screen.width - 1);
				cursorY = Mathf.Clamp(cursorY - vector.y, 0f, Screen.height - 1);
				link.PushInput(Proto.InputType.Cursor, 0, (int)cursorX, (int)cursorY);
			}
			while (text.Count > 0)
			{
				link.PushInput(Proto.InputType.Text, 0, text.Dequeue());
			}
		}
		else
		{
			text.Clear();
			float num = sensitivity * 0.6f + 0.2f;
			float num2 = num * num * num * 8f * 0.15f;
			camera.Yaw = Mathf.DeltaAngle(0f, camera.Yaw + vector.x * num2);
			camera.Pitch = Mathf.Clamp(camera.Pitch - vector.y * num2, -90f, 90f);
		}
		PumpKeys(current);
		// A click used to resume/focus PEAK must not become a Minecraft attack/use.
		// Re-arm only after every physical mouse button has been released.
		if (waitForMouseRelease)
		{
			if (!current2.leftButton.isPressed && !current2.middleButton.isPressed && !current2.rightButton.isPressed && !current2.backButton.isPressed && !current2.forwardButton.isPressed)
				waitForMouseRelease = false;
		}
		else
		{
			PumpButton(1, current2.leftButton); PumpButton(2, current2.middleButton); PumpButton(3, current2.rightButton);
			PumpButton(4, current2.backButton); PumpButton(5, current2.forwardButton);
		}
		float y = current2.scroll.ReadValue().y;
		if (y != 0f)
		{
			link.PushInput(Proto.InputType.Scroll, 0, (y > 0f) ? 120 : (-120));
		}
	}

	private void PumpKeys(Keyboard keyboard)
	{
		bool keyDown = false;
		foreach (KeyControl allKey in keyboard.allKeys)
		{
			if (allKey == null)
			{
				continue;
			}
			Key keyCode = allKey.keyCode;
			bool isPressed = allKey.isPressed;
			if (!screenOpen)
			{
				// Reserve the mode key in gameplay. L is Minecraft's advancements key;
				// forwarding it before the handoff would open a hidden screen there.
				// Inside chat/inventory it remains an ordinary Minecraft key.
				if (Plugin.Instance.CoopMode.IsToggleKey(keyCode))
				{
					if (held.Remove(keyCode) && Scancodes.FromKey.TryGetValue(keyCode, out var toggleScan))
						link.PushInput(Proto.InputType.Key, toggleScan, 0);
					continue;
				}
				if (keyCode == Key.Escape)
				{
					continue;
				}
				if (keyCode == interactKey.Value)
				{
					keyDown = isPressed;
					continue;
				}
				if (keyCode == menuKey.Value)
				{
					if (isPressed && held.Add(keyCode))
					{
						ReleaseKeys();
						held.Add(keyCode);
						link.PushInput(Proto.InputType.OpenMenu, 0);
					}
					else if (!isPressed)
					{
						held.Remove(keyCode);
					}
					continue;
				}
			}
			if (!Scancodes.FromKey.TryGetValue(keyCode, out var value))
			{
				continue;
			}
			if (isPressed && held.Add(keyCode))
			{
				link.PushInput(Proto.InputType.Key, value, 1);
				repeatKey = keyCode;
				repeatAt = Time.unscaledTime + 0.4f;
			}
			else if (!isPressed && held.Remove(keyCode))
			{
				link.PushInput(Proto.InputType.Key, value);
				if (repeatKey == keyCode)
				{
					repeatKey = Key.None;
				}
			}
		}
		if (screenOpen && repeatKey != Key.None && Time.unscaledTime >= repeatAt && Scancodes.FromKey.TryGetValue(repeatKey, out var value2))
		{
			repeatAt = Time.unscaledTime + 0.04f;
			link.PushInput(Proto.InputType.Key, value2, 1);
		}
		interact.Update(keyDown);
	}

	private void PumpButton(int sdlButton, ButtonControl control)
	{
		bool flag = control?.isPressed ?? false;
		if (flag != buttons[sdlButton])
		{
			buttons[sdlButton] = flag;
			link.PushInput(Proto.InputType.MouseButton, (ushort)sdlButton, flag ? 1 : 0);
		}
	}

	private void HookText(Keyboard keyboard)
	{
		if (textSource != keyboard)
		{
			if (textSource != null)
			{
				textSource.onTextInput -= OnText;
			}
			textSource = keyboard;
			keyboard.onTextInput += OnText;
		}
	}

	private void OnText(char c)
	{
		if (routing && screenOpen && c >= ' ' && c != '\u007f' && text.Count < 256)
		{
			text.Enqueue(c);
		}
	}

	private void ReleaseKeys()
	{
		scratch.Clear();
		scratch.AddRange(held);
		foreach (Key item in scratch)
		{
			if (Scancodes.FromKey.TryGetValue(item, out var value))
			{
				link.PushInput(Proto.InputType.Key, value);
			}
		}
		held.Clear();
		repeatKey = Key.None;
	}

	public void ReleaseAll()
	{
		held.Clear();
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i] = false;
		}
		text.Clear();
		repeatKey = Key.None;
		screenOpen = false;
		link.PushInput(Proto.InputType.ReleaseAll, 0);
	}
}
