using System;
using System.Runtime.InteropServices;
using System.Threading;
using BepInEx.Logging;

namespace PeakCraft.Link;

internal static class Rings
{
	public unsafe delegate void MessageHandler(uint type, byte* payload, uint payloadBytes);

	public unsafe static void SeqlockWrite<T>(byte* slot, in T value) where T : unmanaged
	{
		uint num = *(uint*)slot;
		Volatile.Write(ref *(uint*)slot, num + 1);
		Thread.MemoryBarrier();
		fixed (T* ptr = &value)
		{
			Buffer.MemoryCopy((byte*)ptr + 4, slot + 4, sizeof(T) - 4, sizeof(T) - 4);
		}
		Volatile.Write(ref *(uint*)slot, num + 2);
	}

	public unsafe static bool SeqlockRead<T>(byte* slot, out T value) where T : unmanaged
	{
		for (int i = 0; i < 64; i++)
		{
			uint num = Volatile.Read(ref *(uint*)slot);
			if ((num & 1) != 0)
			{
				Thread.SpinWait(1);
				continue;
			}
			T val = *(T*)slot;
			Thread.MemoryBarrier();
			if (Volatile.Read(ref *(uint*)slot) == num)
			{
				value = val;
				return true;
			}
		}
		value = default;
		return false;
	}

	public unsafe static bool EntryPush<T>(byte* ring, int entries, in T entry) where T : unmanaged
	{
		ulong num = *(ulong*)ring;
		ulong num2 = Volatile.Read(ref *(ulong*)(ring + 64));
		if (num - num2 >= (ulong)entries)
		{
			return false;
		}
		((T*)(ring + 128))[num & (ulong)(entries - 1)] = entry;
		Volatile.Write(ref *(ulong*)ring, num + 1);
		return true;
	}

	public unsafe static bool EntryPop<T>(byte* ring, int entries, out T entry) where T : unmanaged
	{
		ulong num = Volatile.Read(ref *(ulong*)ring);
		ulong num2 = ((ulong*)ring)[8];
		if (num2 >= num)
		{
			entry = default;
			return false;
		}
		if (num - num2 > (ulong)entries)
		{
			num2 = num - (ulong)entries;
		}
		entry = ((T*)(ring + 128))[num2 & (ulong)(entries - 1)];
		Volatile.Write(ref *(ulong*)(ring + 64), num2 + 1);
		return true;
	}

	public unsafe static bool ByteWrite(byte* ring, long dataBytes, uint type, void* payload, uint payloadBytes)
	{
		byte* ptr = ring + 128;
		ulong num = (ulong)((sizeof(Proto.MsgHeader) + payloadBytes + 7) & -8);
		if (num > (ulong)dataBytes / 2uL)
		{
			return false;
		}
		ulong num2 = *(ulong*)ring;
		ulong num3 = Volatile.Read(ref *(ulong*)(ring + 64));
		ulong num4 = num2 % (ulong)dataBytes;
		ulong num5 = ((num4 + num > (ulong)dataBytes) ? ((ulong)dataBytes - num4) : 0);
		if ((ulong)(dataBytes - (long)(num2 - num3)) < num + num5)
		{
			return false;
		}
		if (num5 != 0L)
		{
			*(Proto.MsgHeader*)(ptr + num4) = new Proto.MsgHeader
			{
				type = 0u,
				payloadBytes = 0u
			};
			num2 += num5;
			num4 = 0uL;
		}
		*(Proto.MsgHeader*)(ptr + num4) = new Proto.MsgHeader
		{
			type = type,
			payloadBytes = payloadBytes
		};
		if (payloadBytes != 0)
		{
			Buffer.MemoryCopy(payload, ptr + num4 + sizeof(Proto.MsgHeader), payloadBytes, payloadBytes);
		}
		Volatile.Write(ref *(ulong*)ring, num2 + num);
		return true;
	}

	public unsafe static void ByteDrain(byte* ring, long dataBytes, long maxBytes, MessageHandler handler)
	{
		byte* ptr = ring + 128;
		ulong num = Volatile.Read(ref *(ulong*)ring);
		ulong num2 = ((ulong*)ring)[8];
		ulong num3 = 0uL;
		while (num2 < num && num3 < (ulong)maxBytes)
		{
			ulong num4 = num2 % (ulong)dataBytes;
			Proto.MsgHeader* ptr2 = (Proto.MsgHeader*)(ptr + num4);
			if (ptr2->type == 0)
			{
				num2 += (ulong)(dataBytes - (long)num4);
				continue;
			}
			handler(ptr2->type, ptr + num4 + sizeof(Proto.MsgHeader), ptr2->payloadBytes);
			ulong num5 = (ulong)((sizeof(Proto.MsgHeader) + ptr2->payloadBytes + 7) & -8);
			num2 += num5;
			num3 += num5;
		}
		Volatile.Write(ref *(ulong*)(ring + 64), num2);
	}

	public unsafe static bool SelfTest(ManualLogSource log)
	{
		bool flag = true;
		byte* ptr = (byte*)(void*)Marshal.AllocHGlobal((IntPtr)(128L + (long)(8 * sizeof(Proto.InputEvent))));
		byte* ptr2 = (byte*)(void*)Marshal.AllocHGlobal((IntPtr)384L);
		try
		{
			new Span<byte>(ptr, 128).Clear();
			new Span<byte>(ptr2, 128).Clear();
			int num = 0;
			int num2 = 0;
			for (int i = 0; (i < 3) & flag; i++)
			{
				int num3 = 0;
				Proto.InputEvent entry;
				while (num3 < 5)
				{
					bool flag2 = flag;
					entry = new Proto.InputEvent
					{
						type = 1,
						code = (ushort)num,
						a = num * 7,
						b = -num,
						c = (num ^ 0x55)
					};
					flag = flag2 & EntryPush(ptr, 8, in entry);
					num3++;
					num++;
				}
				int num4 = 0;
				while (num4 < 5)
				{
					flag &= EntryPop<Proto.InputEvent>(ptr, 8, out var entry2) && entry2.code == num2 && entry2.a == num2 * 7 && entry2.b == -num2 && entry2.c == (num2 ^ 0x55);
					num4++;
					num2++;
				}
				flag &= !EntryPop<Proto.InputEvent>(ptr, 8, out entry);
			}
			for (int j = 0; j < 8; j++)
			{
				flag &= EntryPush<Proto.InputEvent>(ptr, 8, default(Proto.InputEvent));
			}
			flag &= !EntryPush<Proto.InputEvent>(ptr, 8, default(Proto.InputEvent));
			log.LogInfo((object)("ring self-test: entry ring wrap-around " + (flag ? "ok" : "FAIL")));
			bool bytesOk = true;
			byte* ptr3 = stackalloc byte[40];
			int num5 = 0;
			int read = 0;
			for (int k = 0; (k < 12) & bytesOk; k++)
			{
				int num6 = 0;
				while (num6 < 2)
				{
					for (int l = 0; l < 40; l++)
					{
						ptr3[l] = (byte)(num5 * 31 + l);
					}
					bytesOk &= ByteWrite(ptr2, 256L, (uint)(num5 + 1), ptr3, 40u);
					num6++;
					num5++;
				}
				ByteDrain(ptr2, 256L, long.MaxValue, (uint type, byte* p, uint n) =>
				{
					bytesOk &= type == (uint)(read + 1) && n == 40;
					for (int m = 0; (m < 40) & bytesOk; m++)
					{
						bytesOk &= p[m] == (byte)(read * 31 + m);
					}
					read++;
				});
				bytesOk &= read == num5;
			}
			bytesOk &= (ulong)(*(long*)ptr2) > 256uL;
			log.LogInfo((object)("ring self-test: byte ring wrap-around " + (bytesOk ? "ok" : "FAIL")));
			flag &= bytesOk;
		}
		finally
		{
			Marshal.FreeHGlobal((IntPtr)ptr);
			Marshal.FreeHGlobal((IntPtr)ptr2);
		}
		if (flag)
		{
			log.LogInfo((object)"ring self-test: PASS");
		}
		else
		{
			log.LogError((object)"ring self-test: FAIL");
		}
		return flag;
	}
}
