using System;
using PeakCraft.Player;

static class Program
{
    private static int passed;
    static void Main()
    {
        Test("grounded before any grab cannot finish a climb", () =>
        {
            var gate = new NativeLandingGate();
            Check(!gate.Observe(0, false, true, 0));
            Check(!gate.Observe(10, false, true, 0));
        });
        Test("losing the wall at a ledge does not return on the next physics tick", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, false, 2));
            Check(!gate.Observe(1.02f, false, true, 0));
        });
        Test("upward ledge motion is retained despite a ground contact", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, true, 2));
            Check(!gate.Observe(2, false, true, 1.1f));
            Check(!gate.Observe(2.02f, false, true, 0));
            Check(gate.Observe(2.24f, false, true, 0));
        });
        Test("settled native landing returns after the grace and support interval", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, true, 0));
            Check(!gate.Observe(1.22f, false, true, 0));
            Check(gate.Observe(1.32f, false, true, 0));
        });
        Test("render frames cannot turn a transient contact into a landing", () =>
        {
            var gate = Grab();
            for (int i = 0; i < 500; i++) Check(!gate.Observe(1, false, true, 0));
        });
        Test("losing support restarts the landing interval", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, true, 0));
            Check(!gate.Observe(1.18f, false, false, 0));
            Check(!gate.Observe(1.32f, false, true, 0));
            Check(!gate.Observe(1.5f, false, true, 0));
            Check(gate.Observe(1.54f, false, true, 0));
        });
        Test("airborne slip remains native until an explicit release or landing", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, false, -2));
            Check(!gate.Observe(30, false, false, -15));
        });
        Test("native reattachment discards the previous landing interval", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, true, 0));
            Check(!gate.Observe(1.2f, true, true, 0));
            Check(!gate.Observe(1.3f, false, true, 0));
            Check(!gate.Observe(1.52f, false, true, 0));
            Check(gate.Observe(1.62f, false, true, 0));
        });
        Test("reset prevents a previous character or session from finishing a new request", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, true, 0));
            gate.Reset();
            Check(!gate.Observe(4, false, true, 0));
        });
        Test("non-finite motion is not a settled landing", () =>
        {
            var gate = Grab();
            Check(!gate.Observe(1, false, true, float.NaN));
            Check(!gate.Observe(2, false, true, float.PositiveInfinity));
            Check(!gate.Observe(float.NaN, false, true, 0));
        });
        NativeClimbFlowTests.Run((name, body) => Test(name, body));
        NativeJumpFlowTests.Run((name, body) => Test(name, body));
        Console.WriteLine($"PASS: {passed} native movement tests");
    }
    static NativeLandingGate Grab() { var gate = new NativeLandingGate(); Check(!gate.Observe(0, true, false, 0)); return gate; }
    static void Test(string name, Action body) { body(); passed++; Console.WriteLine("PASS: " + name); }
    static void Check(bool valid) { if (!valid) throw new Exception("native ledge return regression"); }
}
