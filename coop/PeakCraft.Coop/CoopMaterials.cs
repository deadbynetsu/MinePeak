using System.Collections.Generic;
using UnityEngine;

namespace PeakCraft.Coop;

internal static class CoopMaterials
{
	private static readonly string[] Candidates = new string[3] { "Universal Render Pipeline/Particles/Simple Lit", "Universal Render Pipeline/Particles/Lit", "Universal Render Pipeline/Particles/Unlit" };

	private static Shader? shader;

	private static readonly Dictionary<(Texture, bool), Material> Cache = new Dictionary<(Texture, bool), Material>();

	public static float Brightness = 0.6f;

	public static Material? For(Texture texture, bool cutout)
	{
		if (Cache.TryGetValue((texture, cutout), out Material value) && value != null)
		{
			return value;
		}
		if (shader == null)
		{
			string[] candidates = Candidates;
			for (int i = 0; i < candidates.Length; i++)
			{
				shader = Shader.Find(candidates[i]);
				if (shader != null && shader.isSupported)
				{
					break;
				}
			}
			if (shader == null)
			{
				return null;
			}
		}
		value = new Material(shader)
		{
			hideFlags = HideFlags.HideAndDontSave,
			enableInstancing = false
		};
		value.SetTexture("_BaseMap", texture);
		value.mainTexture = texture;
		value.SetColor("_BaseColor", new Color(Brightness, Brightness, Brightness, 1f));
		value.SetFloat("_ColorMode", 0f);
		value.SetFloat("_Cull", 2f);
		value.SetFloat("_Smoothness", 0f);
		value.SetFloat("_SpecularHighlights", 0f);
		value.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
		value.SetColor("_SpecColor", Color.black);
		value.SetFloat("_ReceiveShadows", 1f);
		value.SetFloat("_AlphaClip", 0f);
		value.DisableKeyword("_ALPHATEST_ON");
		if (!cutout)
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
			value.SetFloat("_ZWrite", 1f);
			value.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			value.SetOverrideTag("RenderType", "Transparent");
			value.SetShaderPassEnabled("ShadowCaster", enabled: false);
			value.renderQueue = 2950;
		}
		Cache[(texture, cutout)] = value;
		return value;
	}
}
