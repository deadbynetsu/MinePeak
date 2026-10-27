using System;
using PeakCraft.Hazards;

internal static class Program
{
    private static int passed;

    private static void Main()
    {
        Test("initial PEAK state wins over old Minecraft save", () =>
        {
            var l = new VitalsLedger(); l.Reset(16, 12);
            Command(l, 1, 1, 16, 12);
            l.ObserveGuest(3, 4, 0);
            Equal(l.TargetHealth, 16); Equal(l.TargetFood, 12);
            False(l.Initialized);
            l.ObserveGuest(16, 12, 1);
            True(l.Initialized); False(l.TryGetCommand(out _));
        });
        Test("Minecraft damage and hunger reflect without echo", () =>
        {
            var l = Ready(20, 20);
            l.ObserveGuest(14, 13, 1);
            Equal(l.TargetHealth, 14); Equal(l.TargetFood, 13);
            l.AcknowledgePeakApplied(14, 13); l.SamplePeak(14, 13);
            False(l.TryGetCommand(out _));
        });
        Test("PEAK healing and meal produce additive command", () =>
        {
            var l = Ready(10, 6);
            l.SamplePeak(14, 12);
            Command(l, 2, 2, 4, 6);
            Equal(l.TargetHealth, 14); Equal(l.TargetFood, 12);
            l.AcknowledgePeakApplied(14, 12);
            l.ObserveGuest(14, 12, 2); l.SamplePeak(14, 12);
            False(l.TryGetCommand(out _));
        });
        Test("retransmission retains ID and payload", () =>
        {
            var l = Ready(10, 10); l.SamplePeak(9, 9);
            Command(l, 2, 2, -1, -1);
            Command(l, 2, 2, -1, -1);
            l.ObserveGuest(10, 10, 1);
            Command(l, 2, 2, -1, -1);
        });
        Test("concurrent Minecraft hit and food do not get overwritten", () =>
        {
            var l = Ready(10, 8); l.SamplePeak(12, 11);
            Command(l, 2, 2, 2, 3); l.AcknowledgePeakApplied(12, 11);
            l.ObserveGuest(6, 6, 1); // MC changes before it applies our command
            Equal(l.TargetHealth, 8); Equal(l.TargetFood, 9);
            l.AcknowledgePeakApplied(8, 9);
            l.ObserveGuest(8, 9, 2); // MC added our deltas to its changed values
            Equal(l.TargetHealth, 8); Equal(l.TargetFood, 9);
            False(l.TryGetCommand(out _));
        });
        Test("PEAK changes queue while earlier command is unacknowledged", () =>
        {
            var l = Ready(10, 8); l.SamplePeak(12, 11);
            Command(l, 2, 2, 2, 3); l.AcknowledgePeakApplied(12, 11);
            l.ObserveGuest(6, 6, 1); l.AcknowledgePeakApplied(8, 9);
            l.SamplePeak(7, 8.5f); // another PEAK hit and hunger tick
            Command(l, 2, 2, 2, 3);
            Equal(l.TargetHealth, 7); Equal(l.TargetFood, 8.5f);
            l.ObserveGuest(8, 9, 2);
            Command(l, 3, 2, -1, -0.5f);
            l.AcknowledgePeakApplied(7, 8.5f);
            l.ObserveGuest(7, 8.5f, 3); l.SamplePeak(7, 8.5f);
            False(l.TryGetCommand(out _));
        });
        Test("PEAK changes survive initial acknowledgement", () =>
        {
            var l = new VitalsLedger(); l.Reset(20, 20);
            l.SamplePeak(18, 19); l.AcknowledgePeakApplied(18, 19);
            Command(l, 1, 1, 20, 20);
            l.ObserveGuest(20, 20, 1);
            Equal(l.TargetHealth, 18); Equal(l.TargetFood, 19);
            Command(l, 2, 2, -2, -1);
        });
        Test("PEAK rounded applied statuses cannot create feedback", () =>
        {
            var l = Ready(20, 20);
            l.ObserveGuest(18.7f, 13.3f, 1);
            // PEAK bar quantum 0.025 is half of one MC normalized health/food point.
            l.AcknowledgePeakApplied(18.5f, 13.5f);
            l.SamplePeak(18.5f, 13.5f);
            False(l.TryGetCommand(out _));
            Equal(l.TargetHealth, 18.7f); Equal(l.TargetFood, 13.3f);
        });
        Test("late acknowledgement snapshot cannot roll back accepted state", () =>
        {
            var l = Ready(12, 12); l.SamplePeak(10, 10);
            Command(l, 2, 2, -2, -2);
            l.ObserveGuest(10, 10, 2); l.AcknowledgePeakApplied(10, 10);
            l.ObserveGuest(12, 12, 1);
            Equal(l.TargetHealth, 10); Equal(l.TargetFood, 10);
            l.ObserveGuest(19, 19, 99);
            Equal(l.TargetHealth, 10); Equal(l.TargetFood, 10);
        });
        Test("bounds clamp and idle does not send commands", () =>
        {
            var l = Ready(20, 20);
            for (int i = 0; i < 100; i++)
            {
                l.ObserveGuest(30, -4, 1);
                l.AcknowledgePeakApplied(20, 0); l.SamplePeak(20, 0);
                Equal(l.TargetHealth, 20); Equal(l.TargetFood, 0);
                False(l.TryGetCommand(out _));
            }
        });
        Test("remote healing combines with local healing before acknowledgement", () =>
        {
            var l = Ready(8, 4); l.SamplePeak(12, 9);
            Command(l, 2, 2, 4, 5); l.AcknowledgePeakApplied(12, 9);
            l.ObserveGuest(14, 10, 1);
            Equal(l.TargetHealth, 18); Equal(l.TargetFood, 15);
            l.AcknowledgePeakApplied(18, 15);
            l.ObserveGuest(18, 15, 2);
            False(l.TryGetCommand(out _));
        });
        Test("invalid floats never poison canonical state", () =>
        {
            var l = Ready(14, 10);
            l.ObserveGuest(float.NaN, 1, 1);
            l.SamplePeak(float.PositiveInfinity, float.NaN);
            Equal(l.TargetHealth, 14); Equal(l.TargetFood, 10);
            False(l.TryGetCommand(out _));
        });
        Console.WriteLine($"PASS: {passed} vitals ledger tests");
    }

    private static VitalsLedger Ready(float health, float food)
    {
        var l = new VitalsLedger(); l.Reset(health, food);
        l.ObserveGuest(health, food, 1);
        l.AcknowledgePeakApplied(health, food);
        return l;
    }
    private static void Command(VitalsLedger l, uint id, uint kind, float health, float food)
    {
        True(l.TryGetCommand(out var c));
        if (c.Id != id || c.Kind != kind) throw new Exception($"Expected command {id}/{kind}, got {c.Id}/{c.Kind}");
        Equal(c.Health, health); Equal(c.Food, food);
    }
    private static void Equal(float actual, float expected)
    {
        if (Math.Abs(actual - expected) > 0.0001f) throw new Exception($"Expected {expected}, got {actual}");
    }
    private static void True(bool value) { if (!value) throw new Exception("Expected true"); }
    private static void False(bool value) { if (value) throw new Exception("Expected false"); }
    private static void Test(string name, Action action)
    {
        action(); passed++; Console.WriteLine("PASS: " + name);
    }
}
