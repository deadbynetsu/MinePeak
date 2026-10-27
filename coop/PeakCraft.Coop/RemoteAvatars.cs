using System;
using System.Collections.Generic;
using PeakCraft.Player;
using PeakCraft.World;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeakCraft.Coop;

internal sealed class RemoteAvatars : IDisposable
{
	private sealed class Remote
	{
		public int Actor;

		public Texture2D? Skin;

		public int SkinBytes;
		public byte[] SkinData = Array.Empty<byte>();

		public bool Slim;

		public string Held = "";

		public bool HasPose;

		public float PoseTime;

		public Vector3 Target;

		public Vector3 Feet;

		public float Yaw;

		public float Pitch;

		public float Walk;

		public float WalkRate;

		public float LastWalk;

		public float SwingAmount;

		public int Flags;

		public bool Seen;
	}

	private const float Scale = 15f / 256f;

	private const float PoseTimeout = 2f;

	private readonly CoopNet net;

	private readonly McTextures textures;

	private readonly Dictionary<int, Remote> remotes = new Dictionary<int, Remote>();

	private readonly Dictionary<(string, bool), Mesh> meshes = new Dictionary<(string, bool), Mesh>();

	private Mesh? quad;

	private Mesh? cube;

	private Texture2D? defaultWide;

	private Texture2D? defaultSlim;

	private float nextScan;

	private readonly Dictionary<Vector4, Mesh> quads = new Dictionary<Vector4, Mesh>();

	private readonly Dictionary<Vector4, Mesh> cubes = new Dictionary<Vector4, Mesh>();

	public int Count => remotes.Count;

	public RemoteAvatars(CoopNet net, McTextures textures)
	{
		this.net = net;
		this.textures = textures;
		net.PoseReceived += OnPose;
	}

	public void Dispose()
	{
		net.PoseReceived -= OnPose;
		foreach (var remote in remotes.Values) if (remote.Skin != null) UnityEngine.Object.Destroy(remote.Skin);
		remotes.Clear();
		foreach (var value in quads.Values) UnityEngine.Object.Destroy(value);
		foreach (var value in cubes.Values) UnityEngine.Object.Destroy(value);
		quads.Clear(); cubes.Clear();
		foreach (Mesh value in meshes.Values)
		{
			UnityEngine.Object.Destroy(value);
		}
		meshes.Clear();
	}

	private void OnPose(int actor, object[] d)
	{
		if ((actor == CoopNet.LoopbackActor || IsMinecraftPlayer(actor)) && PosePacket.Valid(d))
		{
			Remote remote = Get(actor);
			Vector3 vector = new Vector3((float)d[1], (float)d[2], (float)d[3]);
			if (actor == -1)
			{
				vector.x += 3f;
			}
			float unscaledTime = Time.unscaledTime;
			float num = (float)d[6];
			if (remote.HasPose && unscaledTime > remote.PoseTime)
			{
				remote.WalkRate = Mathf.Max(0f, num - remote.LastWalk) / (unscaledTime - remote.PoseTime);
			}
			remote.LastWalk = num;
			remote.Target = vector;
			if (!remote.HasPose || (remote.Feet - vector).sqrMagnitude > 25f)
			{
				remote.Feet = vector;
			}
			remote.Yaw = (float)d[4];
			remote.Pitch = (float)d[5];
			remote.Walk = num;
			remote.Flags = (int)d[7];
			remote.HasPose = true;
			remote.PoseTime = unscaledTime;
		}
	}

	private Remote Get(int actor)
	{
		if (!remotes.TryGetValue(actor, out Remote value))
		{
			value = new Remote
			{
				Actor = actor
			};
			remotes[actor] = value;
		}
		return value;
	}

	public bool IsMinecraftPlayer(int actor)
	{
		return net.Prop(actor, CoopProto.PropProto) is int version && version == CoopProto.Version && net.Prop(actor, "pcc.mode") as string == "mc";
	}

	public void Update()
	{
		if (Time.unscaledTime >= nextScan)
		{
			nextScan = Time.unscaledTime + 0.5f;
			Scan();
		}
		foreach (Remote value in remotes.Values)
		{
			Draw(value);
		}
	}

	private void Scan()
	{
		foreach (Remote value in remotes.Values)
		{
			value.Seen = false;
		}
		if (net.Share && PhotonNetwork.InRoom)
		{
			Photon.Realtime.Player[] playerListOthers = PhotonNetwork.PlayerListOthers;
			foreach (Photon.Realtime.Player player in playerListOthers)
			{
				if (IsMinecraftPlayer(player.ActorNumber))
				{
					Refresh(Get(player.ActorNumber));
				}
			}
		}
		if (net.Loopback && net.Share)
		{
			Refresh(Get(-1));
		}
		List<int> list = new List<int>();
		foreach (Remote value2 in remotes.Values)
		{
			if (!value2.Seen)
			{
				list.Add(value2.Actor);
			}
		}
		foreach (int item in list)
		{
			if (remotes[item].Skin != null)
			{
				UnityEngine.Object.Destroy(remotes[item].Skin);
			}
			remotes.Remove(item);
			Plugin.Log.LogInfo((object)$"coop: Minecraft player {item} left");
		}
	}

	private void Refresh(Remote r)
	{
		r.Seen = true;
		object obj = net.Prop(r.Actor, "pcc.skin");
		int num = ((obj is byte[] array) ? array.Length : 0);
		byte[] data = obj as byte[] ?? Array.Empty<byte>();
		if (!r.SkinData.AsSpan().SequenceEqual(data))
		{
			if (r.Skin != null)
			{
				UnityEngine.Object.Destroy(r.Skin);
			}
			r.Skin = SkinShare.Decode(obj);
			r.SkinBytes = num;
			r.SkinData = data.Length <= 65536 ? (byte[])data.Clone() : Array.Empty<byte>();
			Plugin.Log.LogInfo((object)string.Format("coop: Minecraft player {0}: skin {1} ({2} bytes)", r.Actor, (r.Skin != null) ? "received" : "not shared yet", num));
		}
		object obj2 = net.Prop(r.Actor, "pcc.slim");
		bool flag = default;
		int num2;
		if (obj2 is bool)
		{
			flag = (bool)obj2;
			num2 = 1;
		}
		else
		{
			num2 = 0;
		}
		r.Slim = (byte)((uint)num2 & (flag ? 1u : 0u)) != 0;
		r.Held = (net.Prop(r.Actor, "pcc.held") as string) ?? "";
	}

	private static Character? CharacterOf(int actor)
	{
		foreach (Character allCharacter in Character.AllCharacters)
		{
			if (allCharacter != null && allCharacter.view != null && allCharacter.view.Owner != null && allCharacter.view.Owner.ActorNumber == actor)
			{
				return allCharacter;
			}
		}
		return null;
	}

	private void Draw(Remote r)
	{
		float unscaledTime = Time.unscaledTime;
		bool flag = r.HasPose && unscaledTime - r.PoseTime < 2f;
		if (flag)
		{
			r.Feet = Vector3.Lerp(r.Feet, r.Target, 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime));
		}
		else
		{
			Character character = CharacterOf(r.Actor);
			if (character == null)
			{
				return;
			}
			r.Feet = Follower.PeakFeet(character);
			r.Yaw = Coords.McYawFromUnity(character.data.lookValues.x);
			r.Pitch = 0f - character.data.lookValues.y;
			r.WalkRate = 0f;
		}
		Texture2D texture2D = r.Skin ?? DefaultSkin(r.Slim);
		if (texture2D == null)
		{
			return;
		}
		Material material = CoopMaterials.For(texture2D, cutout: false);
		Material material2 = CoopMaterials.For(texture2D, cutout: true);
		if (material == null || material2 == null)
		{
			return;
		}
		bool flag2 = (r.Flags & 1) != 0;
		float num = Mathf.Clamp01(r.WalkRate / 0.6f / 20f * 4f);
		r.SwingAmount = Mathf.Lerp(r.SwingAmount, flag ? num : 0f, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
		float num2 = r.Walk / 0.6f * 4f * 0.6662f;
		float num3 = Mathf.Cos(num2) * 1.4f * r.SwingAmount * 57.29578f;
		float num4 = Mathf.Cos(num2 + MathF.PI) * r.SwingAmount * 57.29578f;
		Matrix4x4 matrix4x = Matrix4x4.TRS(r.Feet + (flag2 ? (Vector3.down * 0.2f) : Vector3.zero), Quaternion.Euler(0f, r.Yaw + 180f, 0f), Vector3.one * (15f / 256f));
		Matrix4x4 rightArm = Matrix4x4.identity;
		AvatarPart[] all = AvatarParts.All;
		for (int i = 0; i < all.Length; i++)
		{
			AvatarPart part = all[i];
			Quaternion q = part.Swing switch
			{
				Swing.Head => Quaternion.Euler(Mathf.Clamp(r.Pitch, -89f, 89f), 0f, 0f),
				Swing.RightArm => Quaternion.Euler(num4, 0f, 0f),
				Swing.LeftArm => Quaternion.Euler(0f - num4, 0f, 0f),
				Swing.RightLeg => Quaternion.Euler(0f - num3, 0f, 0f),
				Swing.LeftLeg => Quaternion.Euler(num3, 0f, 0f),
				_ => Quaternion.identity,
			};
			Matrix4x4 matrix4x2 = Matrix4x4.Translate(part.Pivot) * Matrix4x4.Rotate(q) * Matrix4x4.Translate(-part.Pivot);
			Matrix4x4 matrix4x3 = matrix4x * matrix4x2;
			if (part.Id == "right_arm")
			{
				rightArm = matrix4x3;
			}
			Graphics.DrawMesh(MeshFor(part, r.Slim), matrix4x3, part.Overlay ? material2 : material, 0, null, 0, null, (!part.Overlay) ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows: true);
		}
		DrawHeld(r, rightArm);
	}

	private Texture2D? DefaultSkin(bool slim)
	{
		if (slim)
		{
			return defaultSlim ?? (defaultSlim = textures.DefaultSkin(slim: true));
		}
		return defaultWide ?? (defaultWide = textures.DefaultSkin(slim: false));
	}

	private void DrawHeld(Remote r, Matrix4x4 rightArm)
	{
		if (r.Held.Length == 0)
		{
			return;
		}
		int num = textures.ItemTile(r.Held, out var isBlock);
		textures.Flush();
		if (num < 0)
		{
			return;
		}
		Material material = CoopMaterials.For(textures.Atlas, cutout: true);
		if (!(material == null))
		{
			Vector4 r2 = McTextures.TileRect(num);
			Matrix4x4 matrix = rightArm * Matrix4x4.Translate(new Vector3(6f, 12.5f, 1f));
			Mesh mesh;
			if (isBlock)
			{
				mesh = Cube(r2);
				matrix *= Matrix4x4.TRS(new Vector3(0f, -1f, 3f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 6f);
			}
			else
			{
				mesh = Quad(r2);
				matrix *= Matrix4x4.TRS(new Vector3(0f, -2f, 4f), Quaternion.Euler(0f, 90f, -45f), Vector3.one * 12f);
			}
			Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, null, ShadowCastingMode.Off, receiveShadows: true);
		}
	}

	private Mesh MeshFor(AvatarPart part, bool slim)
	{
		if (meshes.TryGetValue((part.Id, slim), out Mesh value))
		{
			return value;
		}
		value = BuildBox(slim ? part.SlimFrom : part.From, slim ? part.SlimSize : part.Size, part.Uv, part.Inflate);
		value.name = "PeakCraftCoop " + part.Id;
		meshes[(part.Id, slim)] = value;
		return value;
	}

	private static Mesh BuildBox(Vector3 from, Vector3 size, Vector2Int uv, float inflate)
	{
		Vector3 vector = from - Vector3.one * inflate;
		Vector3 vector2 = from + size + Vector3.one * inflate;
		float num = uv.x;
		float num2 = uv.y;
		float x = size.x;
		float y = size.y;
		float z = size.z;
		List<Vector3> verts = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> tris = new List<int>();
		Face(new Vector3(vector2.x, vector2.y, vector2.z), new Vector3(vector.x, vector2.y, vector2.z), new Vector3(vector.x, vector.y, vector2.z), new Vector3(vector2.x, vector.y, vector2.z), num + z, num2 + z, num + z + x, num2 + z + y, Vector3.forward);
		Face(new Vector3(vector2.x, vector2.y, vector.z), new Vector3(vector2.x, vector2.y, vector2.z), new Vector3(vector2.x, vector.y, vector2.z), new Vector3(vector2.x, vector.y, vector.z), num, num2 + z, num + z, num2 + z + y, Vector3.right);
		Face(new Vector3(vector.x, vector2.y, vector2.z), new Vector3(vector.x, vector2.y, vector.z), new Vector3(vector.x, vector.y, vector.z), new Vector3(vector.x, vector.y, vector2.z), num + z + x, num2 + z, num + 2f * z + x, num2 + z + y, Vector3.left);
		Face(new Vector3(vector.x, vector2.y, vector.z), new Vector3(vector2.x, vector2.y, vector.z), new Vector3(vector2.x, vector.y, vector.z), new Vector3(vector.x, vector.y, vector.z), num + 2f * z + x, num2 + z, num + 2f * z + 2f * x, num2 + z + y, Vector3.back);
		Face(new Vector3(vector2.x, vector2.y, vector.z), new Vector3(vector.x, vector2.y, vector.z), new Vector3(vector.x, vector2.y, vector2.z), new Vector3(vector2.x, vector2.y, vector2.z), num + z, num2, num + z + x, num2 + z, Vector3.up);
		Face(new Vector3(vector2.x, vector.y, vector2.z), new Vector3(vector.x, vector.y, vector2.z), new Vector3(vector.x, vector.y, vector.z), new Vector3(vector2.x, vector.y, vector.z), num + z + x, num2, num + z + 2f * x, num2 + z, Vector3.down);
		Mesh mesh = new Mesh
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		mesh.SetVertices(verts);
		mesh.SetUVs(0, uvs);
		Color32[] array = new Color32[verts.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		mesh.colors32 = array;
		mesh.SetTriangles(tris, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
		void Face(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl, float u0, float v0, float u1, float v1, Vector3 outward)
		{
			int count = verts.Count;
			verts.Add(tl);
			verts.Add(tr);
			verts.Add(br);
			verts.Add(bl);
			uvs.Add(P(u0, v0));
			uvs.Add(P(u1, v0));
			uvs.Add(P(u1, v1));
			uvs.Add(P(u0, v1));
			if (Vector3.Dot(Vector3.Cross(tr - tl, br - tl), outward) > 0f)
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
		static Vector2 P(float px, float py)
		{
			return new Vector2(px / 64f, 1f - py / 64f);
		}
	}

	private Mesh Quad(Vector4 r)
	{
		if (quads.TryGetValue(r, out Mesh value))
		{
			return value;
		}
		value = new Mesh
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		value.vertices = new Vector3[4]
		{
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f),
			new Vector3(0.5f, 0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f)
		};
		value.uv = new Vector2[4]
		{
			new Vector2(r.x, r.y),
			new Vector2(r.z, r.y),
			new Vector2(r.z, r.w),
			new Vector2(r.x, r.w)
		};
		value.colors32 = new Color32[4]
		{
			new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
			new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
			new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
			new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue)
		};
		value.triangles = new int[12]
		{
			0, 2, 1, 0, 3, 2, 0, 1, 2, 0,
			2, 3
		};
		value.RecalculateNormals();
		value.RecalculateBounds();
		quads[r] = value;
		quad = value;
		return value;
	}

	private Mesh Cube(Vector4 r)
	{
		if (cubes.TryGetValue(r, out Mesh value))
		{
			return value;
		}
		List<Vector3> list = new List<Vector3>();
		List<Vector2> list2 = new List<Vector2>();
		List<int> list3 = new List<int>();
		Vector3[] array = new Vector3[6]
		{
			Vector3.up,
			Vector3.down,
			Vector3.left,
			Vector3.right,
			Vector3.forward,
			Vector3.back
		};
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector = array[i];
			Vector3 vector2 = Vector3.Cross(vector, (Mathf.Abs(vector.y) > 0.5f) ? Vector3.forward : Vector3.up) * 0.5f;
			Vector3 vector3 = Vector3.Cross(vector, vector2.normalized) * 0.5f;
			Vector3 vector4 = vector * 0.5f;
			int count = list.Count;
			list.Add(vector4 - vector2 - vector3);
			list.Add(vector4 + vector2 - vector3);
			list.Add(vector4 + vector2 + vector3);
			list.Add(vector4 - vector2 + vector3);
			list2.Add(new Vector2(r.x, r.y));
			list2.Add(new Vector2(r.z, r.y));
			list2.Add(new Vector2(r.z, r.w));
			list2.Add(new Vector2(r.x, r.w));
			if (Vector3.Dot(Vector3.Cross(list[count + 1] - list[count], list[count + 2] - list[count]), vector) > 0f)
			{
				list3.Add(count);
				list3.Add(count + 1);
				list3.Add(count + 2);
				list3.Add(count);
				list3.Add(count + 2);
				list3.Add(count + 3);
			}
			else
			{
				list3.Add(count);
				list3.Add(count + 2);
				list3.Add(count + 1);
				list3.Add(count);
				list3.Add(count + 3);
				list3.Add(count + 2);
			}
		}
		value = new Mesh
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		value.SetVertices(list);
		value.SetUVs(0, list2);
		Color32[] array2 = new Color32[list.Count];
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = new Color32(230, 230, 230, byte.MaxValue);
		}
		value.colors32 = array2;
		value.SetTriangles(list3, 0);
		value.RecalculateNormals();
		value.RecalculateBounds();
		cubes[r] = value;
		cube = value;
		return value;
	}
}
