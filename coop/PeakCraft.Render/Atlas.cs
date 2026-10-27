using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace PeakCraft.Render;

internal sealed class Atlas
{
	private static readonly string[] Candidates = new string[3] { "Universal Render Pipeline/Particles/Simple Lit", "Universal Render Pipeline/Particles/Lit", "Universal Render Pipeline/Particles/Unlit" };

	private readonly Shader? shader;

	private readonly float brightness;

	private readonly Dictionary<uint, Texture2D> textures = new Dictionary<uint, Texture2D>();

	private readonly Dictionary<uint, (int width, int height, byte[] alpha)> alphas = new Dictionary<uint, (int, int, byte[])>();

	private readonly Dictionary<ulong, Material> materials = new Dictionary<ulong, Material>();

	private const int MaxStampsPerSize = 128;

	private readonly Dictionary<(int, int), List<Texture2D>> stamps = new Dictionary<(int, int), List<Texture2D>>();

	private readonly Dictionary<(int, int), int> stampUse = new Dictionary<(int, int), int>();

	private int stampFrame = -1;

	public bool Usable => shader != null;

	public Atlas(ConfigFile config)
	{
		ConfigEntry<string> val = config.Bind<string>("Render", "BlockShader", "", "Shader for Minecraft's blocks and skin. Empty picks URP's particle shaders (vertex colour, lit by PEAK).");
		brightness = config.Bind<float>("Render", "BlockBrightness", 0.6f, "Scales the colour of Minecraft's blocks under PEAK's lights (PEAK's sun is brighter than Minecraft's textures expect).").Value;
		if (val.Value.Length > 0)
		{
			shader = Shader.Find(val.Value);
		}
		string[] candidates = Candidates;
		foreach (string name in candidates)
		{
			if (shader != null && shader.isSupported)
			{
				break;
			}
			shader = Shader.Find(name);
		}
		if (shader == null)
		{
			Plugin.Log.LogError((object)"blocks: no usable shader found in PEAK; Minecraft's blocks will not be drawn");
		}
		else
		{
			Plugin.Log.LogInfo((object)("blocks: drawing with PEAK's shader '" + shader.name + "'"));
		}
	}

	public unsafe void SetTexture(uint id, int width, int height, byte* pixels)
	{
		if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
		{
			return;
		}
		if (!textures.TryGetValue(id, out Texture2D value) || value.width != width || value.height != height)
		{
			if (value != null)
			{
				UnityEngine.Object.Destroy(value);
			}
			value = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};
			textures[id] = value;
			foreach (KeyValuePair<ulong, Material> material in materials)
			{
				if ((uint)(material.Key >> 8) == id)
				{
					material.Value.SetTexture("_BaseMap", value);
					material.Value.mainTexture = value;
				}
			}
			Plugin.Log.LogInfo((object)((id == 0) ? $"blocks: atlas {width}x{height}" : $"blocks: texture {id} {width}x{height}"));
		}
		byte[] array = new byte[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = pixels[i * 4 + 3];
		}
		alphas[id] = (width, height, array);
		value.LoadRawTextureData((IntPtr)pixels, width * height * 4);
		value.Apply(updateMipmaps: false);
	}

	public unsafe void SetRegion(int x, int y, int width, int height, byte* pixels)
	{
		if (!textures.TryGetValue(0u, out Texture2D value) || width <= 0 || height <= 0 || x < 0 || y < 0 || x + width > value.width || y + height > value.height)
		{
			return;
		}
		if (Time.frameCount != stampFrame)
		{
			stampFrame = Time.frameCount;
			stampUse.Clear();
		}
		stampUse.TryGetValue((width, height), out var value2);
		stampUse[(width, height)] = value2 + 1;
		if (!stamps.TryGetValue((width, height), out List<Texture2D> value3))
		{
			value3 = new List<Texture2D>();
			stamps[(width, height)] = value3;
		}
		if (value2 < 128)
		{
			if (value2 >= value3.Count)
			{
				value3.Add(new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false)
				{
					hideFlags = HideFlags.HideAndDontSave
				});
			}
			Texture2D texture2D = value3[value2];
			texture2D.LoadRawTextureData((IntPtr)pixels, width * height * 4);
			texture2D.Apply(updateMipmaps: false);
			Graphics.CopyTexture(texture2D, 0, 0, 0, 0, width, height, value, 0, 0, x, y);
		}
	}

	public Surface Classify(uint id, float u0, float v0, float u1, float v1, float u2, float v2)
	{
		if (!alphas.TryGetValue(id, out (int, int, byte[]) value))
		{
			return Surface.Solid;
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i <= 5; i++)
		{
			for (int j = 0; j <= 5 - i; j++)
			{
				float num = ((float)i + 0.25f) / 5.75f;
				float num2 = ((float)j + 0.25f) / 5.75f;
				float num3 = 1f - num - num2;
				int num4 = Mathf.Clamp((int)((u0 * num + u1 * num2 + u2 * num3) * (float)value.Item1), 0, value.Item1 - 1);
				int num5 = Mathf.Clamp((int)((v0 * num + v1 * num2 + v2 * num3) * (float)value.Item2), 0, value.Item2 - 1);
				if (value.Item3[num5 * value.Item1 + num4] < 26)
				{
					flag2 = true;
				}
				else
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			if (!flag2)
			{
				return Surface.Solid;
			}
			return Surface.Cutout;
		}
		return Surface.Empty;
	}

	public Material? Material(uint textureId, Surface surface, bool twoSided)
	{
		if (shader == null || surface == Surface.Empty)
		{
			return null;
		}
		ulong key = ((ulong)textureId << 8) | (ulong)surface | (ulong)(twoSided ? 4 : 0);
		if (materials.TryGetValue(key, out Material value))
		{
			return value;
		}
		value = new Material(shader)
		{
			hideFlags = HideFlags.HideAndDontSave,
			enableInstancing = false
		};
		textures.TryGetValue(textureId, out Texture2D value2);
		value.SetTexture("_BaseMap", value2);
		value.mainTexture = value2;
		value.SetColor("_BaseColor", new Color(brightness, brightness, brightness, 1f));
		value.SetFloat("_ColorMode", 0f);
		value.SetFloat("_Cull", twoSided ? 0f : 2f);
		value.SetFloat("_Smoothness", 0f);
		value.SetFloat("_SpecularHighlights", 0f);
		value.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
		value.SetColor("_SpecColor", Color.black);
		value.SetFloat("_ReceiveShadows", 1f);
		value.SetFloat("_AlphaClip", 0f);
		value.DisableKeyword("_ALPHATEST_ON");
		if (surface == Surface.Solid)
		{
			value.SetFloat("_Surface", 0f);
			value.SetFloat("_SrcBlend", 1f);
			value.SetFloat("_DstBlend", 0f);
			value.SetFloat("_ZWrite", 1f);
			value.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
			value.SetOverrideTag("RenderType", "Opaque");
			value.renderQueue = 2000;
		}
		else
		{
			value.SetFloat("_Surface", 1f);
			value.SetFloat("_Blend", 0f);
			value.SetFloat("_SrcBlend", 5f);
			value.SetFloat("_DstBlend", 10f);
			value.SetFloat("_SrcBlendAlpha", 1f);
			value.SetFloat("_DstBlendAlpha", 10f);
			value.SetFloat("_ZWrite", (surface == Surface.Cutout) ? 1f : 0f);
			value.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			value.SetOverrideTag("RenderType", "Transparent");
			value.SetShaderPassEnabled("ShadowCaster", enabled: false);
			value.renderQueue = 3000 - ((surface == Surface.Cutout) ? 50 : 0);
		}
		materials[key] = value;
		return value;
	}
}
