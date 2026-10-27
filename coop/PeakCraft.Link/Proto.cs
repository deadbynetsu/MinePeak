using System;
using System.Runtime.InteropServices;
using BepInEx.Logging;

namespace PeakCraft.Link;

internal static class Proto
{
	public struct Header
	{
		public uint magic;

		public uint version;

		public uint hostPid;

		public uint guestPid;

		public ulong hostHeartbeatMs;

		public ulong guestHeartbeatMs;
	}

	[Flags]
	public enum HostFlags : uint
	{
		InGame = 1u,
		MenuOpen = 2u,
		Loading = 4u
	}

	public struct HostState
	{
		public uint seq;

		public uint flags;

		public uint worldId;

		public uint collisionEpoch;

		public double posX;

		public double posY;

		public double posZ;

		public float yaw;

		public float pitch;

		public uint teleportSeq;

		public uint viewportW;

		public uint viewportH;

		public float gameHour;
	}

	[Flags]
	public enum GuestFlags : uint
	{
		InWorld = 1u,
		ScreenOpen = 2u,
		OnGround = 4u,
		Sneaking = 8u,
		Sprinting = 0x10u,
		Dead = 0x20u,
		Swimming = 0x40u,
		Flying = 0x80u
	}

	public struct GuestState
	{
		public uint seq;

		public uint flags;

		public double x;

		public double y;

		public double z;

		public float yaw;

		public float pitch;

		public float eyeHeight;

		public float sensitivity;

		public uint teleportAck;

		public uint guiScale;

		public ulong frameCounter;

		public float fovDeg;

		public float bobPhase;

		public float bobAmount;

		public uint pad4C;

		public double eyeX;

		public double eyeY;

		public double eyeZ;

		public long tickQpc;

		public double prevX;

		public double prevY;

		public double prevZ;

		public double curX;

		public double curY;

		public double curZ;

		public float tickEyeO;

		public float tickEye;

		public float walkDistO;

		public float walkDist;

		public float bobO;

		public float bob;

		public float tickMs;

		public uint tickPad;

		public uint cameraMode;

		public float cameraDistance;
	}

	public struct OverlayCtl
	{
		public uint state;

		public uint pad;

		public ulong framesPublished;
	}

	[StructLayout(LayoutKind.Sequential, Size = 64)]
	public struct OverlaySlotHdr
	{
		public uint width;

		public uint height;

		public uint flags;

		public uint pad;

		public ulong frameId;
	}

	public enum InputType : ushort
	{
		Key = 1,
		MouseButton,
		Scroll,
		Cursor,
		Text,
		ReleaseAll,
		Hurt,
		OpenMenu
	}

	public struct InputEvent
	{
		public ushort type;

		public ushort code;

		public int a;

		public int b;

		public int c;
	}

	public struct GuestEvent
	{
		public uint type;

		public uint formId;

		public float a;

		public float b;

		public float c;

		public float d;

		public uint flags;

		public uint weapon;
	}

	public struct MsgHeader
	{
		public uint type;

		public uint payloadBytes;
	}

	public struct ColRegionHdr
	{
		public int minX;

		public int minY;

		public int minZ;

		public int maxX;

		public int maxY;

		public int maxZ;

		public uint epoch;

		public uint count;
	}

	public struct ColBlock
	{
		public int x;

		public int y;

		public int z;

		public uint pad;

		public unsafe fixed ulong bits[8];
	}

	public struct ColTri
	{
		public unsafe fixed float v[9];

		public uint flags;
	}

	public struct RenVertex
	{
		public float x;

		public float y;

		public float z;

		public float u;

		public float v;

		public uint color;

		public uint light;

		public uint flags;
	}

	public struct RenSectionHdr
	{
		public int sx;

		public int sy;

		public int sz;

		public uint vertexCount;
	}

	public struct RenAtlasHdr
	{
		public uint width;

		public uint height;
	}

	public struct RenAtlasRegionHdr
	{
		public uint x;

		public uint y;

		public uint width;

		public uint height;
	}

	public struct RenTextureHdr
	{
		public uint id;

		public uint width;

		public uint height;

		public uint pad;
	}

	public struct RenAvatarHdr
	{
		public uint batchCount;

		public uint vertexCount;
	}

	public struct RenSceneHdr
	{
		public double originX;

		public double originY;

		public double originZ;

		public uint batchCount;

		public uint vertexCount;
	}

	public struct RenBatch
	{
		public uint texture;

		public uint first;

		public uint count;

		public uint flags;
	}

	public const uint Magic = 1129925459u;

	public const uint Version = 11u;

	public const string MappingName = "Local\\PeakCraftCoop_v1";

	public const long OffHeader = 0L;

	public const long OffHostState = 256L;

	public const long OffGuestState = 512L;

	public const long OffOverlayCtl = 768L;

	public const long OffOverlaySlotHdr = 832L;

	public const long OffWaterGrid = 1024L;

	public const long OffInputRing = 4096L;

	public const long OffActorTable = 73728L;

	public const long OffEventRing = 94208L;

	public const long OffWorldEntities = 114688L;

	public const long OffCollisionRing = 131072L;

	public const long CollisionRingBytes = 33554432L;

	public const long OffOverlayPixels = 33685504L;

	public const int MaxOverlayW = 3840;

	public const int MaxOverlayH = 2160;

	public const long OverlaySlotBytes = 33177600L;

	public const int OverlaySlots = 3;

	public const long OffRenderRing = 133218304L;

	public const long RenderRingBytes = 67108864L;

	public const long MappingBytes = 200327168L;

	public const long RingHeadOff = 0L;

	public const long RingTailOff = 64L;

	public const long RingDataOff = 128L;

	public const int InputRingEntries = 4096;

	public const int EventRingEntries = 512;

	public const long ColRingDataBytes = 33554304L;

	public const long RenRingDataBytes = 67108736L;

	public const int MaxActors = 256;

	public const int ActorRecordBytes = 64;

	public const int MaxWorldEntities = 160;

	public const int WorldEntityBytes = 96;

	public const uint OverlayDirty = 4u;

	public const ushort HurtOther = 3;

	public const uint EvHitActor = 1u;

	public const uint EvPlayerDied = 2u;

	public const uint ColPad = 0u;

	public const uint ColClear = 1u;

	public const uint ColRegion = 2u;

	public const uint ColTris = 3u;

	public const uint TriTerrain = 8u;

	public const uint RenPad = 0u;

	public const uint RenAtlas = 1u;

	public const uint RenSection = 2u;

	public const uint RenClearAll = 3u;

	public const uint RenTexture = 4u;

	public const uint RenAvatar = 5u;

	public const uint RenScene = 6u;

	public const uint RenAtlasRegion = 7u;

	public const uint RenRagdoll = 9u;

	public unsafe static bool SelfCheck(ManualLogSource log)
	{
		bool ok = true;
		Check("sizeof(Header)", sizeof(Header), 32L);
		Check("sizeof(HostState)", sizeof(HostState), 64L);
		Check("sizeof(GuestState)", sizeof(GuestState), 200L);
		Check("sizeof(OverlaySlotHdr)", sizeof(OverlaySlotHdr), 64L);
		Check("sizeof(InputEvent)", sizeof(InputEvent), 16L);
		Check("sizeof(GuestEvent)", sizeof(GuestEvent), 32L);
		Check("sizeof(ColRegionHdr)", sizeof(ColRegionHdr), 32L);
		Check("sizeof(ColBlock)", sizeof(ColBlock), 80L);
		Check("sizeof(ColTri)", sizeof(ColTri), 40L);
		Check("sizeof(RenVertex)", sizeof(RenVertex), 32L);
		Check("Header.hostHeartbeatMs", Off<Header>("hostHeartbeatMs"), 16L);
		Check("Header.guestHeartbeatMs", Off<Header>("guestHeartbeatMs"), 24L);
		Check("HostState.posX", Off<HostState>("posX"), 16L);
		Check("HostState.yaw", Off<HostState>("yaw"), 40L);
		Check("HostState.teleportSeq", Off<HostState>("teleportSeq"), 48L);
		Check("HostState.gameHour", Off<HostState>("gameHour"), 60L);
		Check("GuestState.x", Off<GuestState>("x"), 8L);
		Check("GuestState.yaw", Off<GuestState>("yaw"), 32L);
		Check("GuestState.teleportAck", Off<GuestState>("teleportAck"), 48L);
		Check("GuestState.frameCounter", Off<GuestState>("frameCounter"), 56L);
		Check("GuestState.fovDeg", Off<GuestState>("fovDeg"), 64L);
		Check("GuestState.eyeX", Off<GuestState>("eyeX"), 80L);
		Check("GuestState.tickQpc", Off<GuestState>("tickQpc"), 104L);
		Check("GuestState.prevX", Off<GuestState>("prevX"), 112L);
		Check("GuestState.curX", Off<GuestState>("curX"), 136L);
		Check("GuestState.tickEyeO", Off<GuestState>("tickEyeO"), 160L);
		Check("GuestState.tickMs", Off<GuestState>("tickMs"), 184L);
		Check("GuestState.cameraMode", Off<GuestState>("cameraMode"), 192L);
		Check("GuestState.cameraDistance", Off<GuestState>("cameraDistance"), 196L);
		Check("OffHostState", 256L, 256L);
		Check("OffGuestState", 512L, 512L);
		Check("OffOverlayCtl", 768L, 768L);
		Check("OffOverlaySlotHdr", 832L, 832L);
		Check("OffInputRing", 4096L, 4096L);
		Check("OffActorTable", 73728L, 73728L);
		Check("OffEventRing", 94208L, 94208L);
		Check("OffWorldEntities", 114688L, 114688L);
		Check("OffCollisionRing", 131072L, 131072L);
		Check("OffOverlayPixels", 33685504L, 33685504L);
		Check("OffRenderRing", 133218304L, 133218304L);
		Check("MappingBytes", 200327168L, 200327168L);
		Check("InputRing end", 69760L, 69760L);
		Check("ActorTable end", 90176L, 90176L);
		Check("EventRing end", 110720L, 110720L);
		Check("WorldEntities end", 130112L, 130112L);
		if (ok)
		{
			log.LogInfo((object)"protocol layout self-check: PASS");
		}
		else
		{
			log.LogError((object)"protocol layout self-check: FAIL");
		}
		return ok;
		void Check(string what, long actual, long expected)
		{
			if (actual == expected)
			{
				log.LogInfo((object)$"layout ok   {what} = 0x{actual:X}");
			}
			else
			{
				ok = false;
				log.LogError((object)$"layout FAIL {what} = 0x{actual:X}, expected 0x{expected:X}");
			}
		}
		static long Off<T>(string field) where T : notnull
		{
			return Marshal.OffsetOf<T>(field).ToInt64();
		}
	}
}
