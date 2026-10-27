using System;
using System.Collections.Generic;
using PeakCraft.Link;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.Render;

internal sealed class CapturedModel
{
	private readonly Atlas atlas;

	private readonly string name;

	private readonly List<Vector3> positions = new List<Vector3>();

	private readonly List<Vector2> uvs = new List<Vector2>();

	private readonly List<Color32> colours = new List<Color32>();

	private readonly List<int> solid = new List<int>();

	private readonly List<int> cutout = new List<int>();

	private readonly List<int> blended = new List<int>();

	private readonly List<Material> materials = new List<Material>();

	private Mesh? mesh;

	private bool logged;

	public bool Shown { get; private set; }

	public CapturedModel(Atlas atlas, string name)
	{
		this.atlas = atlas;
		this.name = name;
	}

	public void Hide()
	{
		Shown = false;
	}

	public unsafe void Set(uint batchCount, uint vertexCount, Proto.RenBatch* batches, Proto.RenVertex* vertices)
	{
		Shown = batchCount != 0 && vertexCount != 0 && atlas.Usable;
		if (!Shown)
		{
			return;
		}
		if (mesh == null)
		{
			mesh = new Mesh
			{
				hideFlags = HideFlags.HideAndDontSave,
				indexFormat = IndexFormat.UInt32
			};
			mesh.MarkDynamic();
		}
		positions.Clear();
		uvs.Clear();
		colours.Clear();
		materials.Clear();
		for (uint num = 0u; num < vertexCount; num++)
		{
			Proto.RenVertex* ptr = vertices + num;
			positions.Add(new Vector3(ptr->x, ptr->y, 0f - ptr->z));
			uvs.Add(new Vector2(ptr->u, ptr->v));
			uint color = ptr->color;
			colours.Add(new Color32((byte)color, (byte)(color >> 8), (byte)(color >> 16), (byte)(color >> 24)));
		}
		mesh.Clear();
		mesh.SetVertices(positions);
		mesh.SetUVs(0, uvs);
		mesh.SetColors(colours);
		mesh.subMeshCount = (int)(batchCount * 3);
		int sub = 0;
		for (int i = 0; i < batchCount; i++)
		{
			solid.Clear();
			cutout.Clear();
			blended.Clear();
			uint texture = batches[i].texture;
			bool flag = (batches[i].flags & 1) != 0;
			uint num2 = Math.Min(batches[i].first + batches[i].count, vertexCount);
			for (uint num3 = batches[i].first; num3 + 2 < num2; num3 += 3)
			{
				Proto.RenVertex* ptr2 = vertices + num3;
				Surface surface = (flag ? Surface.Translucent : atlas.Classify(texture, ptr2->u, ptr2->v, ptr2[1].u, ptr2[1].v, ptr2[2].u, ptr2[2].v));
				if (surface != Surface.Empty)
				{
					List<int> list = surface switch
					{
						Surface.Cutout => cutout,
						Surface.Solid => solid,
						_ => blended,
					};
					list.Add((int)num3);
					list.Add((int)(num3 + 1));
					list.Add((int)(num3 + 2));
				}
			}
			AddSubmesh(solid, texture, Surface.Solid, ref sub);
			AddSubmesh(cutout, texture, Surface.Cutout, ref sub);
			AddSubmesh(blended, texture, Surface.Translucent, ref sub);
		}
		mesh.subMeshCount = sub;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		if (!logged)
		{
			logged = true;
			Plugin.Log.LogInfo((object)$"{name}: first model received ({batchCount} batches, {vertexCount} vertices)");
		}
	}

	private void AddSubmesh(List<int> indices, uint texture, Surface surface, ref int sub)
	{
		Material material = ((indices.Count > 0) ? atlas.Material(texture, surface, twoSided: true) : null);
		if (!(material == null))
		{
			mesh.SetTriangles(indices, sub++);
			materials.Add(material);
		}
	}

	public void Draw(Matrix4x4 matrix, Camera? camera = null, int layer = 0)
	{
		if (Shown && !(mesh == null))
		{
			for (int i = 0; i < materials.Count && i < mesh.subMeshCount; i++)
			{
				Graphics.DrawMesh(mesh, matrix, materials[i], layer, camera, i, null, ShadowCastingMode.On, receiveShadows: true);
			}
		}
	}
}
