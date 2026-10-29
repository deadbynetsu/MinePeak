using System;
using PeakCraft.Render;
using PeakCraft.World;

static class Program
{
    private static int passed;
    static readonly uint[] Geometry = { 2, 3, 5, 6, 9, 12 };
    static void Main()
    {
        Test("startup discards scene geometry until the Java level is ready", () =>
        {
            var fence = new SceneRenderFence();
            Check(!fence.Ready);
            foreach (uint type in Geometry) Check(!fence.Allow(type));
        });
        Test("atlas resources can load while the scene is blocked", () =>
        {
            var fence = new SceneRenderFence();
            foreach (uint type in new uint[] { 1, 4, 7 }) Check(fence.Allow(type));
        });
        Test("matching ordered readiness marker releases geometry without an optional clear packet", () =>
        {
            var fence = new SceneRenderFence();
            Check(fence.AcceptReady(4, 10, 4, 10));
            Check(fence.Ready);
            foreach (uint type in Geometry) Check(fence.Allow(type));
        });
        Test("a stale airport marker cannot release the island scene", () =>
        {
            var fence = new SceneRenderFence();
            Check(!fence.AcceptReady(4, 10, 9, 11));
            Check(!fence.Ready && !fence.Allow(2));
        });
        Test("a marker from the previous collision generation stays blocked", () =>
        {
            var fence = new SceneRenderFence();
            Check(!fence.AcceptReady(9, 10, 9, 11));
            Check(!fence.Ready && !fence.Allow(12));
        });
        Test("a scene clear before readiness cannot open the gate", () =>
        {
            var fence = new SceneRenderFence();
            Check(!fence.Allow(3)); Check(!fence.Ready);
        });
        Test("scene switch rejects queued geometry until a new boundary", () =>
        {
            var fence = new SceneRenderFence();
            Check(fence.AcceptReady(4, 10, 4, 10));
            fence.RequireScene();
            Check(!fence.Ready && !fence.Allow(2) && !fence.Allow(12));
            Check(!fence.AcceptReady(4, 10, 9, 11));
            Check(fence.AcceptReady(9, 11, 9, 11));
            Check(fence.Allow(2) && fence.Allow(12));
        });
        Test("duplicate marker does not ask the receiver to erase a ready scene", () =>
        {
            var fence = new SceneRenderFence();
            Check(fence.AcceptReady(9, 11, 9, 11));
            Check(!fence.AcceptReady(9, 11, 9, 11));
            Check(fence.Ready && fence.Allow(2));
        });
        Test("new collision epoch requires one fresh receiver reset", () =>
        {
            var fence = new SceneRenderFence();
            Check(fence.AcceptReady(9, 11, 9, 11));
            Check(fence.AcceptReady(9, 12, 9, 12));
            Check(!fence.AcceptReady(9, 12, 9, 12));
        });
        Test("late invalid marker does not close an already ready scene", () =>
        {
            var fence = new SceneRenderFence();
            Check(fence.AcceptReady(9, 11, 9, 11));
            Check(!fence.AcceptReady(4, 10, 9, 11));
            Check(fence.Ready && fence.Allow(6));
        });
        Test("zero world is never accepted as a live scene", () =>
        {
            var fence = new SceneRenderFence();
            Check(!fence.AcceptReady(0, 11, 0, 11)); Check(!fence.Ready);
        });
        Test("readiness packets are never treated as unvalidated geometry", () =>
        {
            var fence = new SceneRenderFence();
            Check(!fence.Allow(13));
            Check(fence.AcceptReady(uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue));
            Check(!fence.Allow(13));
        });
        Test("built-in airport retains its existing world and save identity", () =>
        {
            Check(SceneWorldIdentity.For(3, "Airport") == 4);
            Check(SceneWorldIdentity.For(3, "Renamed Airport") == 4);
        });
        Test("built-in index zero remains a live nonzero world", () =>
        {
            Check(SceneWorldIdentity.For(0, "MainMenu") == 1);
            Check(SceneWorldIdentity.For(84, "Level_85") == 85);
        });
        Test("different Addressables maps never share the missing-build-index zero ID", () =>
        {
            uint first = SceneWorldIdentity.For(-1, "Level_83"), second = SceneWorldIdentity.For(-1, "Level_85");
            Check(first != second && first != 0 && second != 0);
            Check((first & 0x80000000u) != 0 && (second & 0x80000000u) != 0);
        });
        Test("Addressables world IDs are stable FNV-1a UTF-8 values across runs", () =>
        {
            Check(SceneWorldIdentity.For(-1, "Level_85") == 0x876acafdu);
            Check(SceneWorldIdentity.For(-2, "Level_85") == 0x876acafdu);
            Check(SceneWorldIdentity.For(-1, "Level_83") == 0x816ac18bu);
        });
        Test("non-ASCII scene names use UTF-8 rather than platform encoding", () =>
        {
            Check(SceneWorldIdentity.For(-1, "Ilha_á") == 0xf306d71cu);
        });
        Test("scene identity preserves Unity's exact scene-name casing", () =>
        {
            Check(SceneWorldIdentity.For(-1, "Level_85") != SceneWorldIdentity.For(-1, "level_85"));
            Check(SceneWorldIdentity.For(-1, "level_85") == 0xd2aea21du);
        });
        Console.WriteLine($"PASS: {passed} scene render fence and identity tests");
    }
    static void Test(string name, Action body) { body(); passed++; Console.WriteLine("PASS: " + name); }
    static void Check(bool valid) { if (!valid) throw new Exception("scene render fence regression"); }
}
