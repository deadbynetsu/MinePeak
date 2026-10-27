// Test doubles replace Unity, MMF and Photon transport only. The real relay and journal are linked from production.
namespace UnityEngine { internal static class Time { public static float unscaledTime; } }
namespace UnityEngine.SceneManagement
{
    internal readonly struct Scene { public string name => SceneManager.Name; }
    internal static class SceneManager { public static string Name = "Airport"; public static Scene GetActiveScene() => new Scene(); }
}
namespace PeakCraft
{
    internal static class Plugin { public static readonly Logger Log = new Logger(); }
    internal sealed class Logger { public void LogInfo(object text) { } public void LogWarning(object text) { } }
}
namespace PeakCraft.Link
{
    internal static class Proto
    {
        internal enum InputType : ushort { }
        internal struct GuestEvent { public uint type, formId; public float a, b, c, d; public uint flags, weapon; }
    }
    internal sealed class PeakLink
    {
        public readonly Dictionary<(int, int, int), int> Blocks = new();
        public int Inputs;
        public void PushInput(Proto.InputType type, ushort code = 0, int a = 0, int b = 0, int c = 0)
        { if ((ushort)type != 9) throw new Exception("Unexpected input type"); Blocks[(a, b, c)] = code; Inputs++; }
    }
}
namespace PeakCraft.Coop
{
    internal sealed class ViewerBlocks
    {
        public readonly Dictionary<(int, int, int), string> Blocks = new();
        public int Count => Blocks.Count;
        public void Clear() => Blocks.Clear();
        public void Set(int x, int y, int z, string name)
        { if (name == "minecraft:air") Blocks.Remove((x, y, z)); else Blocks[(x, y, z)] = name; }
    }
    internal sealed class CoopNet
    {
        public const int LoopbackActor = -1;
        public readonly Bus Bus;
        public readonly int Actor;
        public int SessionGeneration { get; set; } = 1;
        public int LocalActor => Actor;
        public bool Online => true;
        public bool Share => true;
        public bool Loopback => false;
        public IEnumerable<int> OtherActors => Bus.Peers.Keys.Where(x => x != Actor);
        public event Action<int, object[]>? BlockReceived, SnapshotRequested, SnapshotReceived;
        public CoopNet(Bus bus, int actor) { Bus = bus; Actor = actor; }
        public object? Prop(int actor, string key) => Bus.Peers.ContainsKey(actor) && key == CoopProto.PropProto ? CoopProto.Version : null;
        public void Send(byte code, object[] data, bool reliable, bool cache)
        {
            if (cache) throw new Exception("Room-cache replay must not replace bounded snapshots");
            foreach (int peer in OtherActors.ToArray()) SendToActor(code, data, peer);
        }
        public bool SendToActor(byte code, object[] data, int actor)
        { if (!Bus.Peers.ContainsKey(actor)) return false; Bus.Wire.Add((code, Actor, actor, Clone(data))); return true; }
        private static object[] Clone(object[] data) => data.Select(x => x is object[] a ? Clone(a) : x).ToArray();
        public void Receive(byte code, int actor, object[] packet)
        {
            if (code == CoopProto.PhotonBlockSet) BlockReceived?.Invoke(actor, packet);
            if (code == CoopProto.PhotonSnapshotRequest) SnapshotRequested?.Invoke(actor, packet);
            if (code == CoopProto.PhotonSnapshot) SnapshotReceived?.Invoke(actor, packet);
        }
    }
    internal sealed class Endpoint
    {
        public readonly CoopNet Net;
        public readonly ViewerBlocks Viewer = new();
        public readonly PeakCraft.Link.PeakLink Link = new();
        public readonly BlockRelay Relay;
        public string Scene = "Airport";
        public bool Linked = true;
        public Endpoint(Bus bus, int actor)
        {
            Net = new CoopNet(bus, actor);
            Relay = new BlockRelay(Net, Viewer, Link, () => Linked);
            Activate(); Relay.Update();
        }
        public void Activate() => UnityEngine.SceneManagement.SceneManager.Name = Scene;
        public void Frame() { Activate(); Relay.Update(); }
        public unsafe void Write(int x, int y, int z, int state, string name)
        {
            Activate();
            var text = new PeakCraft.Link.Proto.GuestEvent { type = 6, formId = (2u << 16) | 1u };
            byte* bytes = (byte*)(&text.a);
            var encoded = System.Text.Encoding.UTF8.GetBytes(name);
            if (encoded.Length > 23) throw new Exception("Test helper expects a single string event");
            for (int i = 0; i < encoded.Length; i++) bytes[i] = encoded[i];
            Relay.OnMinecraftEvent(text);
            var block = new PeakCraft.Link.Proto.GuestEvent { type = 7, formId = (uint)state,
                a = BitConverter.Int32BitsToSingle(x), b = BitConverter.Int32BitsToSingle(y), c = BitConverter.Int32BitsToSingle(z) };
            Link.Blocks[(x, y, z)] = state; // The local Minecraft edit happened before its event.
            Relay.OnMinecraftEvent(block);
        }
    }
    internal sealed class Bus
    {
        public readonly Dictionary<int, Endpoint> Peers = new();
        public readonly List<(byte Code, int Sender, int Target, object[] Packet)> Wire = new();
        public Endpoint Add(int actor) { var e = new Endpoint(this, actor); Peers.Add(actor, e); return e; }
        public void Deliver(bool reverse = false)
        {
            var batch = Wire.ToArray(); Wire.Clear();
            if (reverse) Array.Reverse(batch);
            foreach (var p in batch)
                if (Peers.TryGetValue(p.Target, out var peer)) { peer.Activate(); peer.Net.Receive(p.Code, p.Sender, p.Packet); }
        }
        public void Frames(int count, bool reverse = false)
        {
            for (int i = 0; i < count; i++)
            {
                UnityEngine.Time.unscaledTime += 1f / 60;
                Deliver(reverse);
                foreach (var peer in Peers.Values) peer.Frame();
                Deliver(reverse);
            }
        }
    }
}
