using System.Collections.Generic;
using BepInEx.Configuration;
using PeakCraft.Link;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PeakCraft.Render;

internal sealed class RenderRing
{
	private const long BytesPerFrame = 25165824L;

	private readonly PeakLink link;

	private readonly Atlas atlas;

	private readonly BlockSections sections;

	private readonly BlockCollisionSections blockPhysics = new BlockCollisionSections();

	private readonly CapturedModel avatar;

	private readonly CapturedModel body;

	private readonly CapturedModel scene;

	private readonly WorldEntities entities;

	private readonly List<Mirror> mirrors = new List<Mirror>();

	private Vector3 sceneOrigin;

	private float mirrorsFoundAt = -10f;

	private readonly Rings.MessageHandler handler;

	private Mesh? outline;

	private Material? outlineMaterial;

	private int unknownLogged;

	private float lastLog;

	private int messages;

	public unsafe RenderRing(PeakLink link, ConfigFile config)
	{
		this.link = link;
		atlas = new Atlas(config);
		sections = new BlockSections(atlas);
		avatar = new CapturedModel(atlas, "avatar");
		body = new CapturedModel(atlas, "mirror body");
		scene = new CapturedModel(atlas, "scene");
		entities = new WorldEntities(link, atlas);
		handler = Handle;
	}

	public void Update(bool visible, Vector3 feet, float mcYaw, bool firstPerson, bool blocksOnly = false)
	{
		link.DrainRender(25165824L, handler);
		// Keep the actual Minecraft section meshes when native PEAK owns the body.
		// Avatar, first-person items, selection and Minecraft HUD stay under ownership control.
		sections.SetVisible(visible || blocksOnly);
		if (visible)
		{
			if (!firstPerson)
			{
				avatar.Draw(Matrix4x4.Translate(feet));
			}
			DrawInMirrors(feet, mcYaw, firstPerson);
			scene.Draw(Matrix4x4.Translate(sceneOrigin));
			entities.Draw();
			DrawSelection();
			if (Time.unscaledTime - lastLog > 30f && messages > 0)
			{
				Plugin.Log.LogInfo((object)$"blocks: {sections.Count} sections live, {messages} render messages in the last {Time.unscaledTime - lastLog:F0} s");
				lastLog = Time.unscaledTime;
				messages = 0;
			}
		}
	}

	public void Reset()
	{
		sections.Clear();
		blockPhysics.Clear();
		avatar.Hide();
		body.Hide();
		scene.Hide();
	}

	public void ClearPhysics() => blockPhysics.Clear();
	public void DescribePhysics() => blockPhysics.Describe();
	public void Dispose() { Reset(); blockPhysics.Dispose(); }

	private void DrawInMirrors(Vector3 feet, float mcYaw, bool firstPerson)
	{
		if (!firstPerson || (!avatar.Shown && !body.Shown))
		{
			return;
		}
		if (Time.unscaledTime - mirrorsFoundAt > 2f)
		{
			mirrorsFoundAt = Time.unscaledTime;
			mirrors.Clear();
			mirrors.AddRange(Object.FindObjectsByType<Mirror>(FindObjectsSortMode.None));
		}
		CapturedModel capturedModel = (avatar.Shown ? avatar : body);
		Matrix4x4 matrix = (avatar.Shown ? Matrix4x4.Translate(feet) : Matrix4x4.TRS(feet, Quaternion.Euler(0f, mcYaw, 0f), Vector3.one));
		foreach (Mirror mirror in mirrors)
		{
			if (!(mirror == null) && !(mirror.mirrorCamera == null) && mirror.isActiveAndEnabled)
			{
				int cullingMask = mirror.mirrorCamera.cullingMask;
				int i;
				for (i = 0; i < 31 && (cullingMask & (1 << i)) == 0; i++)
				{
				}
				capturedModel.Draw(matrix, mirror.mirrorCamera, i);
			}
		}
	}

	private unsafe void Handle(uint type, byte* payload, uint bytes)
	{
		messages++;
		ulong num2;
		switch (type)
		{
		case 3u:
			sections.Clear();
			blockPhysics.Clear();
			avatar.Hide();
			scene.Hide();
			break;
		case 1u:
			if (bytes >= sizeof(Proto.RenAtlasHdr))
			{
				if ((ulong)bytes >= (ulong)(sizeof(Proto.RenAtlasHdr) + (long)((Proto.RenAtlasHdr*)payload)->width * (long)((Proto.RenAtlasHdr*)payload)->height * 4))
				{
					atlas.SetTexture(0u, (int)((Proto.RenAtlasHdr*)payload)->width, (int)((Proto.RenAtlasHdr*)payload)->height, payload + sizeof(Proto.RenAtlasHdr));
				}
				break;
			}
			goto default;
		case 7u:
			if (bytes >= sizeof(Proto.RenAtlasRegionHdr))
			{
				if ((ulong)bytes >= (ulong)(sizeof(Proto.RenAtlasRegionHdr) + (long)((Proto.RenAtlasRegionHdr*)payload)->width * (long)((Proto.RenAtlasRegionHdr*)payload)->height * 4))
				{
					atlas.SetRegion((int)((Proto.RenAtlasRegionHdr*)payload)->x, (int)((Proto.RenAtlasRegionHdr*)payload)->y, (int)((Proto.RenAtlasRegionHdr*)payload)->width, (int)((Proto.RenAtlasRegionHdr*)payload)->height, payload + sizeof(Proto.RenAtlasRegionHdr));
				}
				break;
			}
			goto default;
		case 4u:
			if (bytes >= sizeof(Proto.RenTextureHdr))
			{
				if (((Proto.RenTextureHdr*)payload)->id != 0 && (ulong)bytes >= (ulong)(sizeof(Proto.RenTextureHdr) + (long)((Proto.RenTextureHdr*)payload)->width * (long)((Proto.RenTextureHdr*)payload)->height * 4))
				{
					atlas.SetTexture(((Proto.RenTextureHdr*)payload)->id, (int)((Proto.RenTextureHdr*)payload)->width, (int)((Proto.RenTextureHdr*)payload)->height, payload + sizeof(Proto.RenTextureHdr));
					Plugin.Instance.SkinShare?.OnTexture(((Proto.RenTextureHdr*)payload)->pad, (int)((Proto.RenTextureHdr*)payload)->width, (int)((Proto.RenTextureHdr*)payload)->height, payload + sizeof(Proto.RenTextureHdr));
				}
				break;
			}
			goto default;
		case 2u:
			if (bytes >= sizeof(Proto.RenSectionHdr))
			{
				if ((ulong)bytes >= (ulong)(sizeof(Proto.RenSectionHdr) + ((Proto.RenSectionHdr*)payload)->vertexCount * sizeof(Proto.RenVertex)))
				{
					sections.SetSection(in *(Proto.RenSectionHdr*)payload, (Proto.RenVertex*)(payload + sizeof(Proto.RenSectionHdr)));
				}
				break;
			}
			goto default;
		case 5u:
			if (bytes >= sizeof(Proto.RenAvatarHdr))
			{
				goto IL_020e;
			}
			goto default;
		case 9u:
			if (bytes >= sizeof(Proto.RenAvatarHdr))
			{
				goto IL_020e;
			}
			goto default;
		case 6u:
			if (bytes >= sizeof(Proto.RenSceneHdr))
			{
				ulong num = (ulong)(sizeof(Proto.RenSceneHdr) + ((Proto.RenSceneHdr*)payload)->batchCount * sizeof(Proto.RenBatch) + ((Proto.RenSceneHdr*)payload)->vertexCount * sizeof(Proto.RenVertex));
				if (bytes >= num)
				{
					Proto.RenBatch* ptr = (Proto.RenBatch*)(payload + sizeof(Proto.RenSceneHdr));
					sceneOrigin = Coords.ToUnity(((Proto.RenSceneHdr*)payload)->originX, ((Proto.RenSceneHdr*)payload)->originY, ((Proto.RenSceneHdr*)payload)->originZ);
					scene.Set(((Proto.RenSceneHdr*)payload)->batchCount, ((Proto.RenSceneHdr*)payload)->vertexCount, ptr, (Proto.RenVertex*)(ptr + ((Proto.RenSceneHdr*)payload)->batchCount));
				}
				break;
			}
			goto default;
		default:
			if (unknownLogged++ < 5)
			{
				Plugin.Log.LogWarning((object)$"blocks: skipping unknown render message type {type} ({bytes} bytes)");
			}
			break;
		case 8u:
		case 10u:
		case 11u:
			break;
		case BlockCollisionPacket.MessageType:
			if (bytes <= BlockCollisionPacket.HeaderBytes + BlockCollisionPacket.MaxBoxes * BlockCollisionPacket.BoxBytes)
				blockPhysics.Set(new System.ReadOnlySpan<byte>(payload, (int)bytes),
					(uint)(SceneManager.GetActiveScene().buildIndex + 1), Plugin.Instance.Collision.Epoch);
			break;
			IL_020e:
			num2 = (ulong)(sizeof(Proto.RenAvatarHdr) + ((Proto.RenAvatarHdr*)payload)->batchCount * sizeof(Proto.RenBatch) + ((Proto.RenAvatarHdr*)payload)->vertexCount * sizeof(Proto.RenVertex));
			if (bytes >= num2)
			{
				Proto.RenBatch* ptr2 = (Proto.RenBatch*)(payload + sizeof(Proto.RenAvatarHdr));
				((type == 5) ? avatar : body).Set(((Proto.RenAvatarHdr*)payload)->batchCount, ((Proto.RenAvatarHdr*)payload)->vertexCount, ptr2, (Proto.RenVertex*)(ptr2 + ((Proto.RenAvatarHdr*)payload)->batchCount));
			}
			break;
		}
	}

	private void DrawSelection()
	{
		if (!link.ReadSelection(out var min, out var max))
		{
			return;
		}
		if (outline == null)
		{
			Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
			if (shader == null)
			{
				return;
			}
			outlineMaterial = new Material(shader)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			outlineMaterial.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
			outline = new Mesh
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			Vector3[] array = new Vector3[8];
			for (int i = 0; i < 8; i++)
			{
				array[i] = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
			}
			outline.vertices = array;
			outline.SetIndices(new int[24]
			{
				0, 1, 2, 3, 4, 5, 6, 7, 0, 2,
				1, 3, 4, 6, 5, 7, 0, 4, 1, 5,
				2, 6, 3, 7
			}, MeshTopology.Lines, 0);
		}
		Vector3 vector = Coords.ToUnity(min.x, min.y, max.z);
		Vector3 vector2 = Coords.ToUnity(max.x, max.y, min.z);
		Matrix4x4 matrix = Matrix4x4.TRS(vector - Vector3.one * 0.004f, Quaternion.identity, vector2 - vector + Vector3.one * 0.008f);
		Graphics.DrawMesh(outline, matrix, outlineMaterial, 0, null, 0, null, ShadowCastingMode.Off, receiveShadows: false);
	}
}
