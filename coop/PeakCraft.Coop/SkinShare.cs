using UnityEngine;

namespace PeakCraft.Coop;

internal sealed class SkinShare
{
	private readonly CoopNet net;

	private int lastHash;

	public SkinShare(CoopNet net)
	{
		this.net = net;
	}

	public unsafe void OnTexture(uint flags, int width, int height, byte* pixels)
	{
		if ((flags & 1) == 0 || width != 64 || (height != 64 && height != 32))
		{
			return;
		}
		int num = 17;
		for (int i = 0; i < width * height * 4; i += 61)
		{
			num = num * 31 + pixels[i];
		}
		if (num == lastHash)
		{
			return;
		}
		lastHash = num;
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
		Color32[] array = new Color32[width * height];
		for (int j = 0; j < height; j++)
		{
			byte* ptr = pixels + (height - 1 - j) * width * 4;
			for (int k = 0; k < width; k++)
			{
				array[j * width + k] = new Color32(ptr[k * 4], ptr[k * 4 + 1], ptr[k * 4 + 2], ptr[k * 4 + 3]);
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply(updateMipmaps: false);
		byte[] array2 = texture2D.EncodeToPNG();
		Object.Destroy(texture2D);
		bool flag = (flags & 2) != 0;
		net.SetProp("pcc.skin", array2);
		net.SetProp("pcc.slim", flag);
		Plugin.Log.LogInfo((object)string.Format("coop: local skin {0}x{1} ({2}), shared as {3} bytes", width, height, flag ? "slim" : "classic", array2.Length));
	}

	public static Texture2D? Decode(object? prop)
	{
		if (!(prop is byte[] array) || array.Length < 24 || array.Length > 65536 ||
			array[0] != 137 || array[1] != 80 || array[2] != 78 || array[3] != 71 ||
			array[16] != 0 || array[17] != 0 || array[18] != 0 || array[19] != 64 ||
			array[20] != 0 || array[21] != 0 || array[22] != 0 || (array[23] != 32 && array[23] != 64))
		{
			return null;
		}
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
		{
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp,
			hideFlags = HideFlags.HideAndDontSave
		};
		if (!texture2D.LoadImage(array) || texture2D.width != 64)
		{
			Object.Destroy(texture2D);
			return null;
		}
		return texture2D;
	}
}
