using System;
using System.Collections.Generic;
using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.Render;

internal sealed class WorldEntities
{
	private const uint Arrow = 1u;

	private const uint Item = 2u;

	private const uint Trident = 3u;

	private const uint Block = 4u;

	private const float Texel = 9f / 160f;

	private readonly PeakLink link;

	private readonly Atlas atlas;

	private readonly List<PeakLink.WorldEntity> entities = new List<PeakLink.WorldEntity>();

	private readonly List<Vector3> positions = new List<Vector3>();

	private readonly List<Vector2> uvs = new List<Vector2>();

	private readonly List<Color32> colours = new List<Color32>();

	private readonly List<int> indices = new List<int>();

	private Mesh? mesh;

	private bool logged;

	public WorldEntities(PeakLink link, Atlas atlas)
	{
		this.link = link;
		this.atlas = atlas;
	}

	public void Draw()
	{
		if (!atlas.Usable || !link.ReadWorldEntities(entities) || entities.Count == 0)
		{
			return;
		}
		positions.Clear();
		uvs.Clear();
		colours.Clear();
		indices.Clear();
		foreach (PeakLink.WorldEntity entity in entities)
		{
			PeakLink.WorldEntity e = entity;
			Vector3 at = Coords.ToUnity(e.x, e.y, e.z);
			switch (e.kind)
			{
			case 1u:
				AddArrow(at, in e);
				break;
			case 2u:
			case 3u:
				AddSprite(at, in e);
				break;
			case 4u:
				AddCube(at, in e);
				break;
			}
		}
		if (indices.Count != 0)
		{
			if (mesh == null)
			{
				mesh = new Mesh
				{
					hideFlags = HideFlags.HideAndDontSave,
					indexFormat = IndexFormat.UInt32
				};
				mesh.MarkDynamic();
			}
			mesh.Clear();
			mesh.SetVertices(positions);
			mesh.SetUVs(0, uvs);
			mesh.SetColors(colours);
			mesh.SetTriangles(indices, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			Material material = atlas.Material(0u, Surface.Cutout, twoSided: true);
			if (material != null)
			{
				Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, null, 0, null, ShadowCastingMode.On, receiveShadows: true);
			}
			if (!logged)
			{
				logged = true;
				Plugin.Log.LogInfo((object)$"entities: drawing Minecraft's arrows and items ({entities.Count} this frame)");
			}
		}
	}

	private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0, float v0, float u1, float v1, Color32 colour)
	{
		int count = positions.Count;
		positions.Add(a);
		positions.Add(b);
		positions.Add(c);
		positions.Add(d);
		uvs.Add(new Vector2(u0, v1));
		uvs.Add(new Vector2(u1, v1));
		uvs.Add(new Vector2(u1, v0));
		uvs.Add(new Vector2(u0, v0));
		for (int i = 0; i < 4; i++)
		{
			colours.Add(colour);
		}
		indices.Add(count);
		indices.Add(count + 1);
		indices.Add(count + 2);
		indices.Add(count);
		indices.Add(count + 2);
		indices.Add(count + 3);
	}

	private void AddArrow(Vector3 at, in PeakLink.WorldEntity e)
	{
		float f = e.yaw * (MathF.PI / 180f);
		float f2 = e.pitch * (MathF.PI / 180f);
		Vector3 vector = new Vector3(Mathf.Sin(f) * Mathf.Cos(f2), Mathf.Sin(f2), (0f - Mathf.Cos(f)) * Mathf.Cos(f2));
		Vector3 normalized = Vector3.Cross((Mathf.Abs(vector.y) > 0.99f) ? Vector3.right : Vector3.up, vector).normalized;
		Vector3 vector2 = Vector3.Cross(vector, normalized);
		Vector3 vector3 = at - vector * 0.67499995f;
		Vector3 vector4 = at + vector * 0.225f;
		Color32 colour = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		for (int i = 0; i < 2; i++)
		{
			Vector3 vector5 = ((i == 0) ? (normalized + vector2) : (normalized - vector2)).normalized * (9f / 64f);
			Quad(vector3 - vector5, vector4 - vector5, vector4 + vector5, vector3 + vector5, e.uv0, e.uv1, e.uv2, e.uv3, colour);
		}
		Vector3 vector6 = at - vector * (99f / 160f);
		Vector3 vector7 = normalized * (9f / 64f);
		Vector3 vector8 = vector2 * (9f / 64f);
		Quad(vector6 - vector7 - vector8, vector6 + vector7 - vector8, vector6 + vector7 + vector8, vector6 - vector7 + vector8, e.uv4, e.uv5, e.uv6, e.uv7, colour);
	}

	private void AddSprite(Vector3 at, in PeakLink.WorldEntity e)
	{
		float num = ((e.scale > 0f) ? e.scale : 0.5f);
		Vector3 vector = Quaternion.Euler(0f, e.yaw, 0f) * Vector3.right * (num * 0.5f);
		Vector3 vector2 = Vector3.up * num;
		Quad(at - vector, at + vector, at + vector + vector2, at - vector + vector2, e.uv0, e.uv1, e.uv2, e.uv3, new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
	}

	private void AddCube(Vector3 at, in PeakLink.WorldEntity e)
	{
		float num = ((e.scale > 0f) ? e.scale : 0.25f);
		Quaternion quaternion = Quaternion.Euler(0f, e.yaw, 0f);
		Vector3 vector = quaternion * Vector3.right * (num * 0.5f);
		Vector3 vector2 = quaternion * Vector3.forward * (num * 0.5f);
		Vector3 vector3 = Vector3.up * num;
		Color32 color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		Color32 colour = ((e.tint != 0) ? new Color32((byte)e.tint, (byte)(e.tint >> 8), (byte)(e.tint >> 16), byte.MaxValue) : color);
		Quad(at - vector - vector2, at + vector - vector2, at + vector - vector2 + vector3, at - vector - vector2 + vector3, e.uv0, e.uv1, e.uv2, e.uv3, color);
		Quad(at + vector - vector2, at + vector + vector2, at + vector + vector2 + vector3, at + vector - vector2 + vector3, e.uv0, e.uv1, e.uv2, e.uv3, color);
		Quad(at + vector + vector2, at - vector + vector2, at - vector + vector2 + vector3, at + vector + vector2 + vector3, e.uv0, e.uv1, e.uv2, e.uv3, color);
		Quad(at - vector + vector2, at - vector - vector2, at - vector - vector2 + vector3, at - vector + vector2 + vector3, e.uv0, e.uv1, e.uv2, e.uv3, color);
		Quad(at - vector - vector2 + vector3, at + vector - vector2 + vector3, at + vector + vector2 + vector3, at - vector + vector2 + vector3, e.uv4, e.uv5, e.uv6, e.uv7, colour);
		Quad(at - vector + vector2, at + vector + vector2, at + vector - vector2, at - vector - vector2, e.uv8, e.uv9, e.uv10, e.uv11, color);
	}
}
