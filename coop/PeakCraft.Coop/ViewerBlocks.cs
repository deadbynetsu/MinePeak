using System;
using System.Collections.Generic;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.Coop;

internal sealed class ViewerBlocks : IDisposable
{
	private sealed class Section
	{
		public readonly Dictionary<int, string> Blocks = new Dictionary<int, string>();

		public Mesh? Mesh;

		public bool Dirty;
	}

	private const float DrawDistance = 160f;

	private readonly McTextures textures;

	private readonly Dictionary<(int, int, int), Section> sections = new Dictionary<(int, int, int), Section>();

	private readonly List<Vector3> verts = new List<Vector3>();

	private readonly List<Vector2> uvs = new List<Vector2>();

	private readonly List<Color32> colors = new List<Color32>();

	private readonly List<int> solid = new List<int>();

	private readonly List<int> cutout = new List<int>();

	private static readonly (int dx, int dy, int dz, byte shade)[] Dirs = new (int, int, int, byte)[6]
	{
		(0, 1, 0, byte.MaxValue),
		(0, -1, 0, 128),
		(0, 0, -1, 204),
		(0, 0, 1, 204),
		(-1, 0, 0, 153),
		(1, 0, 0, 153)
	};

	public int Count { get; private set; }

	public ViewerBlocks(McTextures textures)
	{
		this.textures = textures;
	}

	public void Dispose()
	{
		Clear();
	}

	public void Clear()
	{
		foreach (Section value in sections.Values)
		{
			if (value.Mesh != null)
			{
				UnityEngine.Object.Destroy(value.Mesh);
			}
		}
		sections.Clear();
		Count = 0;
	}

	public void Set(int x, int y, int z, string name)
	{
		(int, int, int) key = (x >> 4, y >> 4, z >> 4);
		int key2 = (x & 0xF) + 16 * (z & 0xF) + 256 * (y & 0xF);
		bool flag = name.Length == 0 || name == "minecraft:air" || name == "minecraft:cave_air" || name == "minecraft:void_air";
		if (!sections.TryGetValue(key, out Section value))
		{
			if (flag)
			{
				return;
			}
			value = new Section();
			sections[key] = value;
		}
		bool flag2 = value.Blocks.ContainsKey(key2);
		if (flag)
		{
			value.Blocks.Remove(key2);
		}
		else
		{
			value.Blocks[key2] = name;
		}
		Count += ((!flag) ? 1 : 0) - (flag2 ? 1 : 0);
		value.Dirty = true;
		MarkDirty(x - 1, y, z, x & 0xF, 0);
		MarkDirty(x + 1, y, z, x & 0xF, 15);
		MarkDirty(x, y - 1, z, y & 0xF, 0);
		MarkDirty(x, y + 1, z, y & 0xF, 15);
		MarkDirty(x, y, z - 1, z & 0xF, 0);
		MarkDirty(x, y, z + 1, z & 0xF, 15);
	}

	private void MarkDirty(int x, int y, int z, int local, int edge)
	{
		if (local == edge && sections.TryGetValue((x >> 4, y >> 4, z >> 4), out Section value))
		{
			value.Dirty = true;
		}
	}

	private bool Solid(int x, int y, int z)
	{
		if (!sections.TryGetValue((x >> 4, y >> 4, z >> 4), out Section value))
		{
			return false;
		}
		int key = (x & 0xF) + 16 * (z & 0xF) + 256 * (y & 0xF);
		if (value.Blocks.TryGetValue(key, out string value2))
		{
			return !textures.Faces(value2).Cutout;
		}
		return false;
	}

	public void Update()
	{
		if (sections.Count == 0)
		{
			return;
		}
		Camera main = Camera.main;
		Material material = (textures.Available ? CoopMaterials.For(textures.Atlas, cutout: false) : null);
		Material material2 = (textures.Available ? CoopMaterials.For(textures.Atlas, cutout: true) : null);
		int num = 0;
		foreach (KeyValuePair<(int, int, int), Section> section in sections)
		{
			Section value = section.Value;
			if (value.Dirty && num < 8)
			{
				Build(section.Key, value);
				num++;
			}
			if (value.Mesh == null || material == null || material2 == null)
			{
				continue;
			}
			Vector3 vector = Coords.ToUnity(section.Key.Item1 * 16 + 8, section.Key.Item2 * 16 + 8, section.Key.Item3 * 16 + 8);
			if (!(main != null) || !((main.transform.position - vector).sqrMagnitude > 25600f))
			{
				Graphics.DrawMesh(value.Mesh, Matrix4x4.identity, material, 0, null, 0, null, ShadowCastingMode.On, receiveShadows: true);
				if (value.Mesh.subMeshCount > 1)
				{
					Graphics.DrawMesh(value.Mesh, Matrix4x4.identity, material2, 0, null, 1, null, ShadowCastingMode.Off, receiveShadows: true);
				}
			}
		}
		textures.Flush();
	}

	private void Build((int, int, int) key, Section s)
	{
		s.Dirty = false;
		verts.Clear();
		uvs.Clear();
		colors.Clear();
		solid.Clear();
		cutout.Clear();
		foreach (KeyValuePair<int, string> block in s.Blocks)
		{
			int num = block.Key & 0xF;
			int num2 = (block.Key >> 4) & 0xF;
			int num3 = block.Key >> 8;
			int num4 = key.Item1 * 16 + num;
			int num5 = key.Item2 * 16 + num3;
			int num6 = key.Item3 * 16 + num2;
			McTextures.BlockFaces blockFaces = textures.Faces(block.Value);
			(int, int, int, byte)[] dirs = Dirs;
			for (int i = 0; i < dirs.Length; i++)
			{
				var (num7, num8, num9, shade) = dirs[i];
				if (!Solid(num4 + num7, num5 + num8, num6 + num9))
				{
					int tile;
					if (num8 > 0)
					{
						tile = blockFaces.Top;
					}
					else
					{
						tile = ((num8 < 0) ? blockFaces.Bottom : blockFaces.Side);
					}
					AddFace(num4, num5, num6, num7, num8, num9, shade, tile, blockFaces.Cutout ? cutout : solid);
				}
			}
		}
		if ((object)s.Mesh == null)
		{
			s.Mesh = new Mesh
			{
				name = "PeakCraftCoop section",
				indexFormat = IndexFormat.UInt32,
				hideFlags = HideFlags.HideAndDontSave
			};
		}
		s.Mesh.Clear();
		s.Mesh.SetVertices(verts);
		s.Mesh.SetUVs(0, uvs);
		s.Mesh.SetColors(colors);
		s.Mesh.subMeshCount = 2;
		s.Mesh.SetTriangles(solid, 0, calculateBounds: false);
		s.Mesh.SetTriangles(cutout, 1, calculateBounds: false);
		s.Mesh.RecalculateNormals();
		s.Mesh.RecalculateBounds();
	}

	private void AddFace(int x, int y, int z, int dx, int dy, int dz, byte shade, int tile, List<int> tris)
	{
		Vector3 vector;
		Vector3 vector2;
		Vector3 vector3;
		Vector3 vector4;
		if (dy != 0)
		{
			float y2 = ((dy > 0) ? (y + 1) : y);
			vector = new Vector3(x, y2, z);
			vector2 = new Vector3(x + 1, y2, z);
			vector3 = new Vector3(x + 1, y2, z + 1);
			vector4 = new Vector3(x, y2, z + 1);
		}
		else if (dz != 0)
		{
			float z2 = ((dz > 0) ? (z + 1) : z);
			vector = new Vector3(x, y, z2);
			vector2 = new Vector3(x + 1, y, z2);
			vector3 = new Vector3(x + 1, y + 1, z2);
			vector4 = new Vector3(x, y + 1, z2);
		}
		else
		{
			float x2 = ((dx > 0) ? (x + 1) : x);
			vector = new Vector3(x2, y, z);
			vector2 = new Vector3(x2, y, z + 1);
			vector3 = new Vector3(x2, y + 1, z + 1);
			vector4 = new Vector3(x2, y + 1, z);
		}
		int count = verts.Count;
		Vector3[] array = new Vector3[4] { vector, vector2, vector3, vector4 };
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector5 = array[i];
			verts.Add(Coords.ToUnity(vector5.x, vector5.y, vector5.z));
			colors.Add(new Color32(shade, shade, shade, byte.MaxValue));
		}
		Vector4 vector6 = ((tile >= 0) ? McTextures.TileRect(tile) : new Vector4(0f, 0f, 0f, 0f));
		uvs.Add(new Vector2(vector6.x, vector6.y));
		uvs.Add(new Vector2(vector6.z, vector6.y));
		uvs.Add(new Vector2(vector6.z, vector6.w));
		uvs.Add(new Vector2(vector6.x, vector6.w));
		Vector3 lhs = Vector3.Cross(verts[count + 1] - verts[count], verts[count + 2] - verts[count]);
		Vector3 rhs = new Vector3(dx, dy, -dz);
		if (Vector3.Dot(lhs, rhs) > 0f)
		{
			tris.Add(count);
			tris.Add(count + 1);
			tris.Add(count + 2);
			tris.Add(count);
			tris.Add(count + 2);
			tris.Add(count + 3);
		}
		else
		{
			tris.Add(count);
			tris.Add(count + 2);
			tris.Add(count + 1);
			tris.Add(count);
			tris.Add(count + 3);
			tris.Add(count + 2);
		}
	}
}
