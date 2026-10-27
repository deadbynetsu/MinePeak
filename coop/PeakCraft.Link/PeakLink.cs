using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using BepInEx.Logging;
using UnityEngine;

namespace PeakCraft.Link;

internal sealed class PeakLink
{
	public struct WorldEntity
	{
		public uint kind;

		public uint id;

		public float x;

		public float y;

		public float z;

		public float yaw;

		public float pitch;

		public float scale;

		public float ext0;

		public float ext1;

		public float ext2;

		public float uv0;

		public float uv1;

		public float uv2;

		public float uv3;

		public float uv4;

		public float uv5;

		public float uv6;

		public float uv7;

		public float uv8;

		public float uv9;

		public float uv10;

		public float uv11;

		public uint tint;
	}

	private const uint PageReadWrite = 4u;

	private const uint FileMapAllAccess = 983071u;

	private const int ErrorAlreadyExists = 183;

	private const ulong GuestTimeoutMs = 3000uL;

	private readonly ManualLogSource log;

	private IntPtr mapping;

	private unsafe byte* @base;

	private Proto.HostState hostState;

	private int overlayFront = 2;

	public unsafe bool Created => @base != null;

	public long QpcFrequency { get; }

	public static ulong TickCount => GetTickCount64();

	private unsafe Proto.Header* Header => (Proto.Header*)@base;

	public unsafe bool GuestAlive
	{
		get
		{
			ulong num = Volatile.Read(ref Header->guestHeartbeatMs);
			if (num != 0L)
			{
				return GetTickCount64() - num < 3000;
			}
			return false;
		}
	}

	public unsafe uint GuestPid => Header->guestPid;

	public unsafe long CollisionBacklog
	{
		get
		{
			byte* ptr = @base + 131072;
			return *(long*)ptr - (long)Volatile.Read(ref *(ulong*)(ptr + 64));
		}
	}

	public unsafe byte* FrontPixels => @base + 33685504 + 33177600L * (long)overlayFront;

	public unsafe Proto.OverlaySlotHdr* FrontHeader => (Proto.OverlaySlotHdr*)(@base + 832 + sizeof(Proto.OverlaySlotHdr) * overlayFront);

	[DllImport("kernel32", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
	private static extern IntPtr CreateFileMappingW(IntPtr file, IntPtr attributes, uint protect, uint sizeHigh, uint sizeLow, string name);

	[DllImport("kernel32", SetLastError = true)]
	private static extern IntPtr MapViewOfFile(IntPtr mapping, uint access, uint offsetHigh, uint offsetLow, UIntPtr bytes);

	[DllImport("kernel32")]
	private static extern bool UnmapViewOfFile(IntPtr view);

	[DllImport("kernel32")]
	private static extern bool CloseHandle(IntPtr handle);

	[DllImport("kernel32")]
	private static extern ulong GetTickCount64();

	[DllImport("kernel32")]
	private static extern bool QueryPerformanceCounter(out long count);

	[DllImport("kernel32")]
	private static extern bool QueryPerformanceFrequency(out long frequency);

	[DllImport("kernel32")]
	private static extern uint GetCurrentProcessId();

	public PeakLink(ManualLogSource log)
	{
		this.log = log;
		QueryPerformanceFrequency(out var frequency);
		QpcFrequency = frequency;
	}

	public static long Qpc()
	{
		QueryPerformanceCounter(out var count);
		return count;
	}

	public unsafe bool Create(string name)
	{
		if (@base != null)
		{
			return true;
		}
		long num = 200327168L;
		mapping = CreateFileMappingW(new IntPtr(-1), IntPtr.Zero, 4u, (uint)(num >> 32), (uint)(num & 0xFFFFFFFFu), name);
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (mapping == IntPtr.Zero)
		{
			log.LogError((object)$"shared memory {name}: CreateFileMapping failed (Windows error {lastWin32Error})");
			return false;
		}
		@base = (byte*)(void*)MapViewOfFile(mapping, 983071u, 0u, 0u, UIntPtr.Zero);
		if (@base == null)
		{
			log.LogError((object)$"shared memory {name}: MapViewOfFile failed (Windows error {Marshal.GetLastWin32Error()})");
			CloseHandle(mapping);
			mapping = IntPtr.Zero;
			return false;
		}
		Clear(256L, sizeof(Proto.HostState));
		Clear(768L, 256L);
		Clear(1024L, 16L);
		Clear(VitalsWire.HostOffset, 64L);
		Clear(VitalsWire.GuestOffset, 64L);
		Clear(4096L, 128L);
		Clear(131072L, 128L);
		Clear(73728L, 64L);
		Clear(94208L, 128L);
		Clear(114688L, 64L);
		Clear(133218304L, 128L);
		hostState = default;
		overlayFront = 2;
		Header->version = 11u;
		Header->hostPid = GetCurrentProcessId();
		Header->hostHeartbeatMs = GetTickCount64();
		Volatile.Write(ref Header->magic, 1129925459u);
		log.LogInfo((object)string.Format("shared memory {0} ({1} MB, {2})", name, num >> 20, (lastWin32Error == 183) ? "reused" : "created"));
		return true;
	}

	public unsafe void Close()
	{
		if (@base != null)
		{
			Volatile.Write(ref Header->magic, 0u);
			UnmapViewOfFile((IntPtr)@base);
			@base = null;
		}
		if (mapping != IntPtr.Zero)
		{
			CloseHandle(mapping);
			mapping = IntPtr.Zero;
		}
	}

	private unsafe void Clear(long offset, long bytes)
	{
		new Span<byte>(@base + offset, (int)bytes).Clear();
	}

	public unsafe void Heartbeat()
	{
		Volatile.Write(ref Header->hostHeartbeatMs, GetTickCount64());
	}

	public unsafe void WriteHostState(in Proto.HostState state)
	{
		hostState = state;
		Rings.SeqlockWrite(@base + 256, in state);
	}

	public unsafe bool ReadGuestState(out Proto.GuestState state)
	{
		return Rings.SeqlockRead<Proto.GuestState>(@base + 512, out state);
	}

	public unsafe void WriteVitals(in VitalsWire.Host state)
	{
		if (Created) Rings.SeqlockWrite(@base + VitalsWire.HostOffset, in state);
	}

	public unsafe bool ReadVitals(out VitalsWire.Guest state)
	{
		state = default;
		return Created && Rings.SeqlockRead(@base + VitalsWire.GuestOffset, out state);
	}

	public unsafe void PushInput(Proto.InputType type, ushort code = 0, int a = 0, int b = 0, int c = 0)
	{
		byte* ring = @base + 4096;
		Proto.InputEvent entry = new Proto.InputEvent
		{
			type = (ushort)type,
			code = code,
			a = a,
			b = b,
			c = c
		};
		Rings.EntryPush(ring, 4096, in entry);
	}

	public unsafe bool PopEvent(out Proto.GuestEvent e)
	{
		return Rings.EntryPop<Proto.GuestEvent>(@base + 94208, 512, out e);
	}

	public unsafe bool WriteCollision(uint type, void* payload, uint bytes)
	{
		return Rings.ByteWrite(@base + 131072, 33554304L, type, payload, bytes);
	}

	public unsafe void DrainRender(long maxBytes, Rings.MessageHandler handler)
	{
		Rings.ByteDrain(@base + 133218304, 67108736L, maxBytes, handler);
	}

	public unsafe bool ReadSelection(out Vector3 min, out Vector3 max)
	{
		byte* ptr = @base + 114688;
		for (int i = 0; i < 16; i++)
		{
			uint num = Volatile.Read(ref *(uint*)ptr);
			if ((num & 1) == 0)
			{
				uint num2 = ((uint*)ptr)[2];
				float* ptr2 = (float*)ptr + 3;
				min = new Vector3(*ptr2, ptr2[1], ptr2[2]);
				max = new Vector3(ptr2[3], ptr2[4], ptr2[5]);
				Thread.MemoryBarrier();
				if (Volatile.Read(ref *(uint*)ptr) == num)
				{
					return num2 != 0;
				}
			}
		}
		min = (max = default);
		return false;
	}

	public unsafe bool ReadWorldEntities(List<WorldEntity> output)
	{
		byte* ptr = @base + 114688;
		for (int i = 0; i < 16; i++)
		{
			uint num = Volatile.Read(ref *(uint*)ptr);
			if ((num & 1) == 0)
			{
				output.Clear();
				uint num2 = Math.Min(((uint*)ptr)[1], 160u);
				WorldEntity* ptr2 = (WorldEntity*)(ptr + 64);
				for (uint num3 = 0u; num3 < num2; num3++)
				{
					output.Add(ptr2[num3]);
				}
				Thread.MemoryBarrier();
				if (Volatile.Read(ref *(uint*)ptr) == num)
				{
					return true;
				}
			}
		}
		return false;
	}

	public unsafe bool AcquireOverlayFrame()
	{
		ref int reference = ref *(int*)(@base + 768);
		if ((Volatile.Read(ref reference) & 4) == 0)
		{
			return false;
		}
		int num = Interlocked.Exchange(ref reference, overlayFront);
		overlayFront = num & 3;
		return true;
	}

	public unsafe void ResetOverlay()
	{
		Volatile.Write(ref *(int*)(@base + 768), 0);
		overlayFront = 2;
	}
}
