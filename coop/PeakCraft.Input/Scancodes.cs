using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace PeakCraft.Input;

internal static class Scancodes
{
	public const ushort Escape = 41;

	public static readonly Dictionary<Key, ushort> FromKey = Build();

	private static Dictionary<Key, ushort> Build()
	{
		Dictionary<Key, ushort> dictionary = new Dictionary<Key, ushort>();
		for (int i = 0; i < 26; i++)
		{
			dictionary[(Key)(15 + i)] = (ushort)(4 + i);
		}
		for (int j = 0; j < 9; j++)
		{
			dictionary[(Key)(41 + j)] = (ushort)(30 + j);
		}
		dictionary[Key.Digit0] = 39;
		dictionary[Key.Enter] = 40;
		dictionary[Key.Escape] = 41;
		dictionary[Key.Backspace] = 42;
		dictionary[Key.Tab] = 43;
		dictionary[Key.Space] = 44;
		dictionary[Key.Minus] = 45;
		dictionary[Key.Equals] = 46;
		dictionary[Key.LeftBracket] = 47;
		dictionary[Key.RightBracket] = 48;
		dictionary[Key.Backslash] = 49;
		dictionary[Key.Semicolon] = 51;
		dictionary[Key.Quote] = 52;
		dictionary[Key.Backquote] = 53;
		dictionary[Key.Comma] = 54;
		dictionary[Key.Period] = 55;
		dictionary[Key.Slash] = 56;
		dictionary[Key.CapsLock] = 57;
		for (int k = 0; k < 12; k++)
		{
			dictionary[(Key)(94 + k)] = (ushort)(58 + k);
		}
		dictionary[Key.PrintScreen] = 70;
		dictionary[Key.ScrollLock] = 71;
		dictionary[Key.Pause] = 72;
		dictionary[Key.Insert] = 73;
		dictionary[Key.Home] = 74;
		dictionary[Key.PageUp] = 75;
		dictionary[Key.Delete] = 76;
		dictionary[Key.End] = 77;
		dictionary[Key.PageDown] = 78;
		dictionary[Key.RightArrow] = 79;
		dictionary[Key.LeftArrow] = 80;
		dictionary[Key.DownArrow] = 81;
		dictionary[Key.UpArrow] = 82;
		dictionary[Key.NumLock] = 83;
		dictionary[Key.NumpadDivide] = 84;
		dictionary[Key.NumpadMultiply] = 85;
		dictionary[Key.NumpadMinus] = 86;
		dictionary[Key.NumpadPlus] = 87;
		dictionary[Key.NumpadEnter] = 88;
		for (int l = 0; l < 9; l++)
		{
			dictionary[(Key)(85 + l)] = (ushort)(89 + l);
		}
		dictionary[Key.Numpad0] = 98;
		dictionary[Key.NumpadPeriod] = 99;
		dictionary[Key.OEM1] = 100;
		dictionary[Key.ContextMenu] = 101;
		dictionary[Key.NumpadEquals] = 103;
		dictionary[Key.LeftCtrl] = 224;
		dictionary[Key.LeftShift] = 225;
		dictionary[Key.LeftAlt] = 226;
		dictionary[Key.LeftMeta] = 227;
		dictionary[Key.RightCtrl] = 228;
		dictionary[Key.RightShift] = 229;
		dictionary[Key.RightAlt] = 230;
		dictionary[Key.RightMeta] = 231;
		return dictionary;
	}
}
