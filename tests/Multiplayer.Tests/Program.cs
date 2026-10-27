using System.Text.Json;
using PeakCraft.Coop;

var results = new List<object>();
int failed = 0;
void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
void Test(string name, Action action)
{
    try { action(); results.Add(new { test = name, passed = true, detail = "" }); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; results.Add(new { test = name, passed = false, detail = ex.Message }); Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
BlockEdit E(int actor, long revision, int state = 1, string name = "minecraft:stone", int x = 1) => new("Airport", x, -511, 3, state, name, revision, actor);

Test("packet roundtrip preserves coordinates, state, scene and Lamport stamp", () => {
    var edit = E(2, 37); Check(BlockEdit.TryParse(edit.Packet(), out var copy));
    Check(copy.X == 1 && copy.Y == -511 && copy.Z == 3 && copy.Actor == 2 && copy.Revision == 37 && copy.Name == "minecraft:stone");
});
Test("malformed, legacy and oversized block packets are rejected", () => {
    Check(!BlockEdit.TryParse(new object[7], out _));
    foreach (int field in new[] { 0, 1, 3, 4, 5, 6, 8, 9 }) {
        var packet = E(1, 1).Packet(); packet[field] = "wrong type"; Check(!BlockEdit.TryParse(packet, out _), "field " + field);
    }
    Check(!E(0, 1).Valid); Check(!E(1, 0).Valid); Check(!E(1, long.MaxValue).Valid);
    Check(!E(1, 1, name: "minecraft:" + new string('x', 193)).Valid);
    Check(!E(1, 1, state: 65536).Valid); Check(!E(1, 1, x: 30000001).Valid);
    Check(!new BlockEdit("Airport", 0, -1025, 0, 1, "minecraft:stone", 1, 1).Valid);
});
Test("duplicate and stale edits cannot resurrect an old block", () => {
    var j = new BlockJournal(); Check(j.Merge(E(1, 5))); Check(!j.Merge(E(1, 5))); Check(!j.Merge(E(2, 4)));
    Check(j.Merge(E(1, 6, 0, "minecraft:air"))); Check(!j.Merge(E(1, 5))); Check(j.Count == 1);
});
Test("simultaneous conflicting edits converge in opposite delivery orders", () => {
    var a = new BlockJournal(); var b = new BlockJournal(); var low = E(1, 8); var high = E(2, 8, 2, "minecraft:dirt");
    a.Merge(low); a.Merge(high); b.Merge(high); b.Merge(low);
    Check(a.TryGet((1, -511, 3), out var av) && b.TryGet((1, -511, 3), out var bv) && av.Actor == 2 && bv.Actor == 2);
});
Test("receiving a remote revision advances the next local revision", () => {
    var j = new BlockJournal(); j.Merge(E(9, 100)); var local = j.Local("Airport", 1, -511, 3, 1, "minecraft:stone", 1);
    Check(local.Revision == 101 && j.Merge(local));
});
Test("journal bounds new coordinates but allows edits and tombstones at capacity", () => {
    var j = new BlockJournal(); for (int i = 0; i < BlockJournal.Capacity; i++) Check(j.Merge(E(1, i + 1, x: i)));
    Check(!j.Merge(E(1, 100000, x: BlockJournal.Capacity)) && j.Count == BlockJournal.Capacity);
    Check(j.Merge(E(1, 100001, 0, "minecraft:air", x: 0))); j.Clear(); Check(j.Count == 0);
});
Test("pose packets reject NaN, Infinity, wrong numeric types and unknown flags", () => {
    var p = new object[] { CoopProto.Magic, 1f, 2f, 3f, 4f, 5f, 6f, 3 }; Check(PosePacket.Valid(p));
    foreach (object bad in new object[] { float.NaN, float.PositiveInfinity, 1d, "a" }) { p[1] = bad; Check(!PosePacket.Valid(p)); }
    p[1] = 1f; p[7] = 8; Check(!PosePacket.Valid(p));
});
Test("real relay sends live edits between two simulated endpoints", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); a.Write(1, -511, 3, 1, "minecraft:stone"); bus.Frames(4);
    Check(b.Link.Blocks[(1, -511, 3)] == 1 && b.Viewer.Blocks[(1, -511, 3)] == "minecraft:stone");
    Check(a.Relay.Sent == 1 && b.Relay.Received == 1);
});
Test("late join receives blocks created while host was alone and removed air states", () => {
    var bus = new Bus(); var host = bus.Add(1); host.Write(1, -511, 3, 1, "minecraft:stone");
    host.Write(2, -511, 3, 1, "minecraft:stone"); host.Write(2, -511, 3, 0, "minecraft:air");
    var guest = bus.Add(2); bus.Frames(150);
    Check(guest.Link.Blocks[(1, -511, 3)] == 1 && guest.Link.Blocks[(2, -511, 3)] == 0);
    Check(!guest.Viewer.Blocks.ContainsKey((2, -511, 3)) && guest.Relay.JournalCount == 2 && guest.Relay.SnapshotChunksReceived > 0);
});
Test("three endpoints converge after conflicting writes delivered in reverse order", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); var c = bus.Add(3);
    a.Write(1, -511, 3, 1, "minecraft:stone"); b.Write(1, -511, 3, 2, "minecraft:dirt"); c.Write(1, -511, 3, 3, "minecraft:sand");
    bus.Frames(150, reverse: true);
    foreach (var peer in bus.Peers.Values) Check(peer.Link.Blocks[(1, -511, 3)] == 3);
});
Test("large snapshots are chunked, deduplicated and drain through bounded input batches", () => {
    var bus = new Bus(); var host = bus.Add(1);
    for (int i = 0; i < 1050; i++) host.Write(i, -511, 3, 1, "minecraft:stone");
    var guest = bus.Add(2); bus.Frames(240);
    Check(guest.Relay.JournalCount == 1050 && guest.Link.Blocks.Count == 1050 && guest.Relay.PendingInputs == 0);
    Check(guest.Relay.SnapshotChunksReceived >= 33 && guest.Link.Inputs == 1050, "Duplicate snapshot inputs or missing coordinates");
});
Test("viewer-only peer replays remote block states when Minecraft links later", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); b.Linked = false; bus.Frames(2);
    a.Write(1, -511, 3, 1, "minecraft:stone"); bus.Frames(4); Check(b.Link.Blocks.Count == 0 && b.Viewer.Count == 1);
    b.Linked = true; bus.Frames(4); Check(b.Link.Blocks[(1, -511, 3)] == 1);
});
Test("scene isolation rejects delayed airport edits on the island", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); a.Write(1, -511, 3, 1, "minecraft:stone");
    b.Scene = "Level"; b.Activate(); b.Relay.ResetScene(); bus.Frames(4);
    Check(b.Relay.Received == 0 && b.Viewer.Count == 0);
});
Test("snapshot remains available after the original host leaves", () => {
    var bus = new Bus(); var host = bus.Add(1); var survivor = bus.Add(2); host.Write(1, -511, 3, 1, "minecraft:stone"); bus.Frames(150);
    bus.Peers.Remove(1); var late = bus.Add(3); bus.Frames(150);
    Check(late.Link.Blocks[(1, -511, 3)] == 1 && late.Relay.SnapshotChunksReceived > 0);
});
Test("a claimed live edit from a different actor is rejected", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2);
    b.Activate(); b.Net.Receive(CoopProto.PhotonBlockSet, 1, E(3, 1).Packet()); Check(b.Relay.JournalCount == 0);
});
Test("a malformed snapshot chunk is rejected atomically and oversized chunks are refused", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); b.Activate();
    b.Net.Receive(CoopProto.PhotonSnapshot, 1, new object[] { CoopProto.Magic, CoopProto.Version, "Airport", new object[] { E(1, 1).Packet(), new object[1] } });
    Check(b.Relay.JournalCount == 0 && b.Relay.SnapshotChunksReceived == 0);
    var huge = Enumerable.Range(0, 33).Select(i => (object)E(1, i + 1, x: i).Packet()).ToArray();
    b.Net.Receive(CoopProto.PhotonSnapshot, 1, new object[] { CoopProto.Magic, CoopProto.Version, "Airport", huge });
    Check(b.Relay.JournalCount == 0);
});
Test("an old snapshot arriving after a newer live edit cannot undo that edit", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); b.Activate();
    b.Net.Receive(CoopProto.PhotonBlockSet, 1, E(1, 9, 0, "minecraft:air").Packet());
    b.Net.Receive(CoopProto.PhotonSnapshot, 1, new object[] { CoopProto.Magic, CoopProto.Version, "Airport", new object[] { E(1, 3).Packet() } });
    b.Frame(); Check(b.Link.Blocks[(1, -511, 3)] == 0 && b.Viewer.Count == 0 && b.Relay.Received == 1);
});
Test("the production relay emits at most sixteen remote MMF inputs per frame", () => {
    var bus = new Bus(); var a = bus.Add(1); var b = bus.Add(2); b.Activate();
    for (int i = 0; i < 65; i++) b.Net.Receive(CoopProto.PhotonBlockSet, 1, E(1, i + 1, x: i).Packet());
    Check(b.Link.Inputs == 0 && b.Relay.PendingInputs == 65); b.Frame();
    Check(b.Link.Inputs == 16 && b.Relay.PendingInputs == 49); bus.Frames(5); Check(b.Link.Inputs == 65);
});
Test("a new Photon session clears the prior session journal and viewer", () => {
    var bus = new Bus(); var a = bus.Add(1); a.Write(1, -511, 3, 1, "minecraft:stone");
    Check(a.Relay.JournalCount == 1); a.Net.SessionGeneration++; a.Frame();
    Check(a.Relay.JournalCount == 0 && a.Viewer.Count == 0 && a.Relay.PendingInputs == 0);
});

var report = new { scope = "Production BlockRelay/BlockJournal/PosePacket with simulated Photon, Unity and shared-memory transport; not a two-Steam-account playtest", passed = failed == 0, total = results.Count, failed, results };
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
if (args.Length == 2 && args[0] == "--report") File.WriteAllText(args[1], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
return failed == 0 ? 0 : 1;
