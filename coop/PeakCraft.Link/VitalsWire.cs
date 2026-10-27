using System.Runtime.InteropServices;

namespace PeakCraft.Link;

// Optional extension, after the existing 16x16 water grid (ends at 0x810).
// Neither the base protocol layout nor its rings change. Both slots use a seqlock.
internal static class VitalsWire
{
    public const uint Magic = 0x56495431;
    public const long HostOffset = 0x900, GuestOffset = 0xA00;

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 64)]
    internal struct Host
    {
        public uint Seq, Magic, Session, Flags, CommandId, CommandKind;
        public float Health, Food;
        public uint WorldId, Reserved;
        public ulong Heartbeat;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 64)]
    internal struct Guest
    {
        public uint Seq, Magic, Session, Flags, Ack, PlayerEpoch;
        public float Health, Food, MaxHealth, Saturation;
        public ulong Heartbeat;
        public uint DeathCount;
    }
}
