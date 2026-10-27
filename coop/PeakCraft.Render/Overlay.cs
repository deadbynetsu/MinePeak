using System;
using PeakCraft.Link;
using UnityEngine;

namespace PeakCraft.Render;

internal sealed class Overlay
{
	private readonly PeakLink link;

	private Texture2D? texture;

	private Texture2D? cursor;

	private bool topDown;

	private int frames;

	private float lastLog;

	public bool HasFrame => texture != null;

	public Overlay(PeakLink link)
	{
		this.link = link;
	}

	public unsafe void Update()
	{
		if (!link.AcquireOverlayFrame())
		{
			return;
		}
		Proto.OverlaySlotHdr* frontHeader = link.FrontHeader;
		int width = (int)frontHeader->width;
		int height = (int)frontHeader->height;
		if (width <= 0 || height <= 0 || width > 3840 || height > 2160)
		{
			return;
		}
		if (texture == null || texture.width != width || texture.height != height)
		{
			if (texture != null)
			{
				UnityEngine.Object.Destroy(texture);
			}
			texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};
			Plugin.Log.LogInfo((object)$"overlay: {width}x{height} (PEAK is {Screen.width}x{Screen.height})");
		}
		topDown = (frontHeader->flags & 1) == 0;
		texture.LoadRawTextureData((IntPtr)link.FrontPixels, width * height * 4);
		texture.Apply(updateMipmaps: false);
		frames++;
		if (Time.unscaledTime - lastLog > 10f)
		{
			Plugin.Log.LogInfo((object)$"overlay: {(float)frames / Mathf.Max(Time.unscaledTime - lastLog, 0.001f):F0} frames/s from Minecraft, frame id {frontHeader->frameId}");
			lastLog = Time.unscaledTime;
			frames = 0;
		}
	}

	public void Clear()
	{
		if (texture != null)
		{
			UnityEngine.Object.Destroy(texture);
			texture = null;
		}
	}

	public void Draw(bool showCursor, Vector2 cursorPosition)
	{
		if (Event.current.type != EventType.Repaint || texture == null)
		{
			return;
		}
		GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), texture, topDown ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f), alphaBlend: true);
		if (showCursor)
		{
			if ((object)cursor == null)
			{
				cursor = MakeCursor();
			}
			float num = Mathf.Max(2f, (float)Screen.height / 540f);
			GUI.DrawTexture(new Rect(cursorPosition.x, cursorPosition.y, (float)cursor.width * num, (float)cursor.height * num), cursor);
		}
	}

	private static Texture2D MakeCursor()
	{
		Texture2D texture2D = new Texture2D(16, 16, TextureFormat.RGBA32, mipChain: false)
		{
			filterMode = FilterMode.Point,
			hideFlags = HideFlags.HideAndDontSave
		};
		Color32[] array = new Color32[256];
		for (int i = 0; i < 16; i++)
		{
			bool flag;
			int num2;
			Color32 color;
			int num;
			for (int j = 0; j < 16; flag = (byte)num2 != 0, color = ((num != 0) ? (flag ? new Color32(0, 0, 0, byte.MaxValue) : new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue)) : new Color32(0, 0, 0, 0)), array[(15 - i) * 16 + j] = color, j++)
			{
				if ((float)j <= (float)i * 0.7f)
				{
					num = ((i < 12) ? 1 : 0);
					if (num != 0)
					{
						num2 = ((j == 0 || (float)(j + 1) > (float)i * 0.7f || i == 11) ? 1 : 0);
						continue;
					}
				}
				else
				{
					num = 0;
				}
				num2 = 0;
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply();
		return texture2D;
	}
}
