using System;
using System.Collections.Generic;
using PeakCraft.Input;

static class Program
{
    private static int passed;
    static void Main()
    {
        Test("live Minecraft chat remains protected when focus stops routing", () =>
            Check(InputRoutingSafety.MinecraftScreenOpen(true, true, 7)));
        Test("ordinary Minecraft gameplay allows the mode shortcut", () =>
            Check(!InputRoutingSafety.MinecraftScreenOpen(true, true, 5)));
        Test("stale chat after Java disconnect cannot lock the fallback shortcut", () =>
            Check(!InputRoutingSafety.MinecraftScreenOpen(true, false, 7)));
        Test("hidden Minecraft screen cannot block the native PEAK preference", () =>
            Check(!InputRoutingSafety.MinecraftScreenOpen(false, true, 7)));
        Test("startup Minecraft title screen does not block the mode shortcut", () =>
            Check(!InputRoutingSafety.MinecraftScreenOpen(true, true, 2)));
        Test("backpack wheel retains the held G action after native menu takeover", () =>
            Check(InputRoutingSafety.RoutePeakInteract(false, true, false, true)));
        Test("pause menu cannot acquire the backpack interaction action", () =>
            Check(!InputRoutingSafety.RoutePeakInteract(false, false, false, true)));
        Test("Minecraft chat blocks G even with a stale native wheel", () =>
            Check(!InputRoutingSafety.RoutePeakInteract(true, true, true, true)));
        Test("losing focus releases native backpack interaction", () =>
            Check(!InputRoutingSafety.RoutePeakInteract(false, true, false, false)));
        Test("safety release survives repeated full-ring failures", () =>
        {
            var barrier = new InputReleaseBarrier(); barrier.Request();
            for (int i=0; i<5; i++) { Check(!barrier.Flush(() => false)); Check(barrier.Pending); }
            int delivered=0;
            Check(barrier.Flush(() => { delivered++; return true; }));
            Check(!barrier.Pending && delivered==1);
            Check(barrier.Flush(() => throw new Exception("duplicate release")));
        });
        Test("multiple focus or mode changes coalesce into one delivered release", () =>
        {
            var barrier=new InputReleaseBarrier();
            barrier.Request(); Check(!barrier.Flush(() => false)); barrier.Request(); barrier.Request();
            var events=new List<string>();
            if(!barrier.Pending) events.Add("stale attack");
            Check(barrier.Flush(() => { events.Add("release all"); return true; }));
            if(!barrier.Pending) events.Add("current input");
            Check(string.Join(",",events)=="release all,current input");
        });
        Test("failed release transport keeps the safety barrier armed", () =>
        {
            var barrier=new InputReleaseBarrier(); barrier.Request();
            try { barrier.Flush(() => throw new InvalidOperationException("transport interrupted")); }
            catch (InvalidOperationException) { }
            Check(barrier.Pending); Check(barrier.Flush(() => true)); Check(!barrier.Pending);
        });
        Console.WriteLine($"PASS: {passed} input safety tests");
    }
    static void Test(string name,Action body) { body(); passed++; Console.WriteLine("PASS: "+name); }
    static void Check(bool valid) { if(!valid) throw new Exception("input safety regression"); }
}
