using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PeakCraft.Coop;

internal sealed class McTextures : IDisposable
{
	public readonly struct BlockFaces(int top, int side, int bottom, bool cutout)
	{
		public readonly int Top = top;

		public readonly int Side = side;

		public readonly int Bottom = bottom;

		public readonly bool Cutout = cutout;
	}

	private const int Tile = 16;

	private const int AtlasSize = 1024;

	private const int PerRow = 64;

	private readonly CoopConfig config;

	private ZipArchive? jar;

	private bool searched;

	private float nextSearch;

	private readonly Dictionary<string, int> tiles = new Dictionary<string, int>();

	private readonly Dictionary<int, bool> tileCutout = new Dictionary<int, bool>();

	private readonly Dictionary<string, BlockFaces> blocks = new Dictionary<string, BlockFaces>();

	private readonly Dictionary<string, JObject?> models = new Dictionary<string, JObject>();

	private Texture2D? atlas;

	private bool atlasDirty;

	public string? JarPath { get; private set; }

	public bool Available => Open() != null;

	public Texture2D Atlas
	{
		get
		{
			if (atlas == null)
			{
				atlas = new Texture2D(1024, 1024, TextureFormat.RGBA32, mipChain: false, linear: false)
				{
					filterMode = FilterMode.Point,
					wrapMode = TextureWrapMode.Clamp,
					hideFlags = HideFlags.HideAndDontSave,
					name = "PeakCraftCoop atlas"
				};
				Color32[] pixels = new Color32[1048576];
				atlas.SetPixels32(pixels);
				atlas.Apply(updateMipmaps: false);
			}
			return atlas;
		}
	}

	public McTextures(CoopConfig config)
	{
		this.config = config;
	}

	public void Flush()
	{
		if (atlasDirty && atlas != null)
		{
			atlasDirty = false;
			atlas.Apply(updateMipmaps: false);
		}
	}

	public static Vector4 TileRect(int tile)
	{
		float num = 1f / 64f;
		int num2 = tile % 64;
		int num3 = tile / 64;
		return new Vector4((float)num2 * num, 1f - (float)(num3 + 1) * num, (float)(num2 + 1) * num, 1f - (float)num3 * num);
	}

	public void Dispose()
	{
		jar?.Dispose();
		if (atlas != null)
		{
			UnityEngine.Object.Destroy(atlas);
		}
	}

	private ZipArchive? Open()
	{
		if (jar != null)
		{
			return jar;
		}
		if (searched && Time.unscaledTime < nextSearch)
		{
			return null;
		}
		bool alreadySearched = searched;
		searched = true;
		nextSearch = Time.unscaledTime + 5f;
		foreach (string item in Candidates())
		{
			try
			{
				if (File.Exists(item))
				{
					jar = new ZipArchive(File.OpenRead(item), ZipArchiveMode.Read);
					JarPath = item;
					Plugin.Log.LogInfo((object)("coop: textures from " + item));
					return jar;
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning((object)("coop: can't read " + item + ": " + ex.Message));
			}
		}
		if (!alreadySearched)
			Plugin.Log.LogWarning((object)"coop: no Minecraft client jar found yet; retrying every 5 seconds. Other players' blocks and items draw untextured until it is available. Set Coop.MinecraftJar to your jar.");
		return null;
	}

	private IEnumerable<string> Candidates()
	{
		string text = config.CoopMcJar.Value.Trim();
		if (text.Length > 0 && !text.Equals("auto", StringComparison.OrdinalIgnoreCase))
		{
			yield return text;
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		yield return Path.Combine(folderPath, "PeakCraftCoop", "Prism", "libraries", "com", "mojang", "minecraft", "26.3", "minecraft-26.3-client.jar");
		List<string> list = new List<string>();
		string environmentVariable = Environment.GetEnvironmentVariable("PEAKCRAFT_MC_DIR");
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			list.Add(environmentVariable);
		}
		list.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft"));
		foreach (string item in list)
		{
			string path = Path.Combine(item, "versions");
			if (!Directory.Exists(path))
			{
				continue;
			}
			IEnumerable<string> enumerable;
			try
			{
				enumerable = (from d in Directory.GetDirectories(path)
					select Path.Combine(d, Path.GetFileName(d) + ".jar")).Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).ToList();
			}
			catch (Exception)
			{
				continue;
			}
			foreach (string item2 in enumerable)
			{
				yield return item2;
			}
		}
	}

	private string? ReadText(string entry)
	{
		ZipArchiveEntry zipArchiveEntry = Open()?.GetEntry(entry);
		if (zipArchiveEntry == null)
		{
			return null;
		}
		using StreamReader streamReader = new StreamReader(zipArchiveEntry.Open());
		return streamReader.ReadToEnd();
	}

	private byte[]? ReadBytes(string entry)
	{
		ZipArchiveEntry zipArchiveEntry = Open()?.GetEntry(entry);
		if (zipArchiveEntry == null)
		{
			return null;
		}
		using Stream stream = zipArchiveEntry.Open();
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public Texture2D? DefaultSkin(bool slim)
	{
		byte[] array = ReadBytes("assets/minecraft/textures/entity/player/" + (slim ? "slim" : "wide") + "/steve.png") ?? ReadBytes("assets/minecraft/textures/entity/steve.png");
		if (array == null)
		{
			return null;
		}
		return SkinShare.Decode(array);
	}

	public int TileOf(string texture)
	{
		if (tiles.TryGetValue(texture, out var value))
		{
			return value;
		}
		int num = -1;
		byte[] array = ReadBytes("assets/minecraft/textures/" + texture + ".png");
		if (array != null && tiles.Count((KeyValuePair<string, int> t) => t.Value >= 0) < 4096)
		{
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
			if (texture2D.LoadImage(array))
			{
				num = tiles.Count((KeyValuePair<string, int> t) => t.Value >= 0);
				int num2 = Mathf.Min(texture2D.width, texture2D.height);
				Color32[] pixels = texture2D.GetPixels32();
				Color32[] array2 = new Color32[256];
				bool flag = false;
				for (int num3 = 0; num3 < 16; num3++)
				{
					for (int num4 = 0; num4 < 16; num4++)
					{
						int num5 = num4 * num2 / 16;
						int num6 = texture2D.height - num2 + num3 * num2 / 16;
						Color32 color = (array2[num3 * 16 + num4] = pixels[num6 * texture2D.width + num5]);
						flag |= color.a < 250;
					}
				}
				Vector4 vector = TileRect(num);
				Atlas.SetPixels32((int)(vector.x * 1024f), (int)(vector.y * 1024f), 16, 16, array2);
				atlasDirty = true;
				tileCutout[num] = flag;
			}
			UnityEngine.Object.Destroy(texture2D);
		}
		tiles[texture] = num;
		return num;
	}

	public bool IsCutout(int tile)
	{
		bool value = default;
		return (tile >= 0 && tileCutout.TryGetValue(tile, out value)) & value;
	}

	private static string Strip(string id)
	{
		if (!id.StartsWith("minecraft:", StringComparison.Ordinal))
		{
			return id;
		}
		return id.Substring(10);
	}

	private JObject? Model(string id)
	{
		id = Strip(id);
		if (models.TryGetValue(id, out JObject value))
		{
			return value;
		}
		string text = ReadText("assets/minecraft/models/" + id + ".json");
		value = ((text != null) ? JObject.Parse(text) : null);
		models[id] = value;
		return value;
	}

	private Dictionary<string, string> TexturesOf(string modelId)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		List<JObject> list = new List<JObject>();
		string text = modelId;
		int num = 0;
		while (text != null && num < 8)
		{
			JObject jObject = Model(text);
			if (jObject == null)
			{
				break;
			}
			list.Add(jObject);
			text = (string?)jObject["parent"];
			num++;
		}
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			if (list[num2]["textures"] is JObject jObject2)
			{
				foreach (KeyValuePair<string, JToken> item in jObject2)
				{
					dictionary[item.Key] = ((string?)item.Value) ?? "";
				}
			}
		}
		return dictionary;
	}

	private static string? Resolve(Dictionary<string, string> textures, params string[] keys)
	{
		foreach (string key in keys)
		{
			string text = (textures.TryGetValue(key, out string value) ? value : null);
			int num = 0;
			while (text != null && text.StartsWith("#", StringComparison.Ordinal) && num < 8)
			{
				text = (textures.TryGetValue(text.Substring(1), out string value2) ? value2 : null);
				num++;
			}
			if (!string.IsNullOrEmpty(text))
			{
				return Strip(text);
			}
		}
		return null;
	}

	private string? FirstModel(string block)
	{
		string text = ReadText("assets/minecraft/blockstates/" + Strip(block) + ".json");
		if (text == null)
		{
			return null;
		}
		JObject jObject = JObject.Parse(text);
		JToken jToken = null;
		if (jObject["variants"] is JObject jObject2)
		{
			jToken = jObject2.Properties().FirstOrDefault()?.Value;
		}
		else if (jObject["multipart"] is JArray { Count: >0 } jArray)
		{
			jToken = jArray[0]["apply"];
		}
		if (jToken is JArray jArray2)
		{
			jToken = ((jArray2.Count > 0) ? jArray2[0] : null);
		}
		return (string?)jToken?["model"];
	}

	public BlockFaces Faces(string block)
	{
		if (blocks.TryGetValue(block, out var value))
		{
			return value;
		}
		int num = -1;
		int num2 = -1;
		int bottom = -1;
		if (Available)
		{
			string modelId = FirstModel(block) ?? ("block/" + Strip(block));
			Dictionary<string, string> textures = TexturesOf(modelId);
			string text = Resolve(textures, "side", "all", "texture", "cross", "wall", "particle");
			string text2 = Resolve(textures, "top", "end", "up", "all", "texture", "cross", "particle");
			string text3 = Resolve(textures, "bottom", "end", "down", "top", "all", "texture", "cross", "particle");
			num = ((text2 != null) ? TileOf(text2) : (-1));
			num2 = ((text != null) ? TileOf(text) : (-1));
			bottom = ((text3 != null) ? TileOf(text3) : (-1));
			if (num2 < 0)
			{
				num2 = TileOf("block/" + Strip(block));
			}
			num = ((num < 0) ? num2 : num);
			bottom = ((bottom < 0) ? num2 : bottom);
			Plugin.Log.LogInfo((object)("coop: textures: " + block + " -> " + (text2 ?? "?") + " / " + (text ?? "?") + " / " + (text3 ?? "?")));
		}
		value = new BlockFaces(num, num2, bottom, IsCutout(num2) || IsCutout(num));
		blocks[block] = value;
		return value;
	}

	public int ItemTile(string item, out bool isBlock)
	{
		isBlock = false;
		if (!Available || item.Length == 0)
		{
			return -1;
		}
		string text = Resolve(TexturesOf("item/" + Strip(item)), "layer0");
		if (text != null)
		{
			return TileOf(text);
		}
		isBlock = ReadText("assets/minecraft/blockstates/" + Strip(item) + ".json") != null;
		if (!isBlock)
		{
			return -1;
		}
		return Faces(item).Side;
	}
}
