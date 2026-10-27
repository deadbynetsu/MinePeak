using System.Collections.Generic;
using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.Render;

internal sealed class BlockSections
{
	private readonly Atlas atlas;

	private readonly Dictionary<(int, int, int), GameObject> sections = new Dictionary<(int, int, int), GameObject>();

	private readonly List<Vector3> positions = new List<Vector3>();

	private readonly List<Vector2> uvs = new List<Vector2>();

	private readonly List<Color32> colours = new List<Color32>();

	private readonly List<int> solid = new List<int>();

	private readonly List<int> cutout = new List<int>();

	private readonly List<int> blended = new List<int>();

	private GameObject? root;

	public int Count => sections.Count;

	public BlockSections(Atlas atlas)
	{
		this.atlas = atlas;
	}

	public void SetVisible(bool visible)
	{
		if (root != null && root.activeSelf != visible)
		{
			root.SetActive(visible);
		}
	}

	public void Clear()
	{
		foreach (GameObject value in sections.Values)
		{
			Remove(value);
		}
		sections.Clear();
	}

	private static void Remove(GameObject section)
	{
		MeshFilter component = section.GetComponent<MeshFilter>();
		if (component != null && component.sharedMesh != null)
		{
			Object.Destroy(component.sharedMesh);
		}
		Object.Destroy(section);
	}

	public unsafe void SetSection(in Proto.RenSectionHdr header, Proto.RenVertex* vertices)
	{
		(int, int, int) key = (header.sx, header.sy, header.sz);
		if (sections.TryGetValue(key, out GameObject value))
		{
			Remove(value);
			sections.Remove(key);
		}
		int num = (int)header.vertexCount / 3 * 3;
		if (num == 0 || !atlas.Usable)
		{
			return;
		}
		positions.Clear();
		uvs.Clear();
		colours.Clear();
		solid.Clear();
		cutout.Clear();
		blended.Clear();
		for (int i = 0; i < num; i += 3)
		{
			for (int j = 0; j < 3; j++)
			{
				Proto.RenVertex* ptr = vertices + (i + j);
				positions.Add(new Vector3(ptr->x, ptr->y, 0f - ptr->z));
				uvs.Add(new Vector2(ptr->u, ptr->v));
				uint color = ptr->color;
				colours.Add(new Color32((byte)color, (byte)(color >> 8), (byte)(color >> 16), (byte)(color >> 24)));
			}
			Proto.RenVertex* ptr2 = vertices + i;
			Surface surface = (((ptr2->flags & 2) != 0) ? Surface.Translucent : atlas.Classify(0u, ptr2->u, ptr2->v, ptr2[1].u, ptr2[1].v, ptr2[2].u, ptr2[2].v));
			if (surface != Surface.Empty)
			{
				List<int> list = surface switch
				{
					Surface.Cutout => cutout,
					Surface.Solid => solid,
					_ => blended,
				};
				list.Add(i);
				list.Add(i + 2);
				list.Add(i + 1);
			}
		}
		Mesh mesh = new Mesh
		{
			hideFlags = HideFlags.HideAndDontSave,
			indexFormat = ((num > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16)
		};
		mesh.SetVertices(positions);
		mesh.SetUVs(0, uvs);
		mesh.SetColors(colours);
		List<Material> list2 = new List<Material>(3);
		mesh.subMeshCount = ((solid.Count > 0) ? 1 : 0) + ((cutout.Count > 0) ? 1 : 0) + ((blended.Count > 0) ? 1 : 0);
		int submesh = 0;
		if (solid.Count > 0)
		{
			mesh.SetTriangles(solid, submesh++);
			list2.Add(atlas.Material(0u, Surface.Solid, twoSided: false));
		}
		if (cutout.Count > 0)
		{
			mesh.SetTriangles(cutout, submesh++);
			list2.Add(atlas.Material(0u, Surface.Cutout, twoSided: true));
		}
		if (blended.Count > 0)
		{
			mesh.SetTriangles(blended, submesh);
			list2.Add(atlas.Material(0u, Surface.Translucent, twoSided: false));
		}
		if (list2.Count == 0)
		{
			Object.Destroy(mesh);
			return;
		}
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		if (root == null)
		{
			root = new GameObject("PeakCraft blocks")
			{
				hideFlags = HideFlags.DontSave
			};
			Object.DontDestroyOnLoad(root);
		}
		GameObject gameObject = new GameObject($"section {header.sx} {header.sy} {header.sz}");
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject.transform.position = Coords.ToUnity((double)header.sx * 16.0, (double)header.sy * 16.0, (double)header.sz * 16.0);
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterials = list2.ToArray();
		meshRenderer.shadowCastingMode = ShadowCastingMode.On;
		meshRenderer.receiveShadows = true;
		sections[key] = gameObject;
	}
}
