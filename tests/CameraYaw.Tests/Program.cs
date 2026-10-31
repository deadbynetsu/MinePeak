using System;
using PeakCraft.Player;

static class Program
{
    private static int passed;

    static void Main()
    {
        Test("mouse heading crosses positive 180 continuously", () =>
        {
            var heading = new ContinuousYaw(); heading.Follow(179);
            heading.Turn(2); Near(heading.Value, 181);
        });
        Test("mouse heading crosses negative 180 continuously", () =>
        {
            var heading = new ContinuousYaw(); heading.Follow(-179);
            heading.Turn(-2); Near(heading.Value, -181);
        });
        Test("repeated full mouse turns retain the travelled angle", () =>
        {
            var heading = new ContinuousYaw();
            for (int i = 0; i < 5000; i++) heading.Turn(1);
            Near(heading.Value, 5000);
            for (int i = 0; i < 10000; i++) heading.Turn(-1);
            Near(heading.Value, -5000);
        });
        Test("native handoff crosses positive seam on the nearest revolution", () =>
        {
            var heading = new ContinuousYaw(); heading.Turn(899);
            heading.Follow(-179); Near(heading.Value, 901);
        });
        Test("native handoff crosses negative seam on the nearest revolution", () =>
        {
            var heading = new ContinuousYaw(); heading.Turn(-899);
            heading.Follow(179); Near(heading.Value, -901);
        });
        Test("native rotation remains continuous through several revolutions", () =>
        {
            var heading = new ContinuousYaw(); heading.Follow(170);
            for (int i = 171; i <= 1250; i++)
            {
                heading.Follow(Wrap(i)); Near(heading.Value, i);
            }
        });
        Test("large single mouse movement is not mistaken for a native shortest turn", () =>
        {
            var heading = new ContinuousYaw(); heading.Turn(270);
            Near(heading.Value, 270); heading.Follow(-89); Near(heading.Value, 271);
        });
        Test("full-turn absolute headings preserve the local revolution", () =>
        {
            var heading = new ContinuousYaw(); heading.Turn(1081);
            heading.Follow(721); Near(heading.Value, 1081);
        });
        Test("invalid host inputs cannot poison subsequent camera frames", () =>
        {
            var heading = new ContinuousYaw(); heading.Turn(45);
            heading.Turn(float.NaN); heading.Turn(float.PositiveInfinity);
            heading.Follow(float.NaN); heading.Follow(float.NegativeInfinity);
            Near(heading.Value, 45); heading.Turn(1); Near(heading.Value, 46);
        });
        Test("vanilla hand inertia has no wrap impulse across either compass seam", () =>
        {
            // Minecraft 26.3 interpolates yBob halfway towards yaw each tick,
            // then rotates the hand by (viewYaw - interpolatedBob) * 0.1.
            // Exercise the linked production heading against that renderer input.
            foreach (int direction in new[] { -1, 1 })
            {
                var heading = new ContinuousYaw(); heading.Follow(direction * 170);
                float bob = heading.Value;
                for (int frame = 0; frame < 80; frame++)
                {
                    heading.Turn(direction * 0.5f);
                    var previousBob = bob;
                    bob += (heading.Value - bob) * 0.5f;
                    foreach (float partialTick in new[] { 0f, .5f, 1f })
                    {
                        float renderBob = previousBob + (bob - previousBob) * partialTick;
                        float handYaw = (heading.Value - renderBob) * .1f;
                        Check(Math.Abs(handYaw) < .11f);
                    }
                }
            }
            // Establish that the same scenario detects the original normalized host.
            float oldBob = 179.5f;
            float oldView = Wrap(180.5f);
            oldBob += (oldView - oldBob) * .5f;
            Check(Math.Abs((oldView - oldBob) * .1f) > 17f);
        });
        Test("legitimate hand inertia remains and settles after a turn", () =>
        {
            var heading = new ContinuousYaw(); heading.Follow(179);
            float bob = heading.Value;
            heading.Turn(10);
            bob += (heading.Value - bob) * .5f;
            float movingSway = (heading.Value - bob) * .1f;
            Check(movingSway > .4f && movingSway < .6f);
            for (int tick = 0; tick < 15; tick++) bob += (heading.Value - bob) * .5f;
            Check(Math.Abs((heading.Value - bob) * .1f) < .001f);
        });
        Console.WriteLine($"PASS: {passed} continuous camera yaw tests");
    }

    static float Wrap(float degrees) => (float)(degrees - Math.Floor((degrees + 180) / 360.0) * 360.0);
    static void Test(string name, Action body) { body(); passed++; Console.WriteLine("PASS: " + name); }
    static void Near(float actual, float expected) => Check(Math.Abs(actual - expected) < .002f);
    static void Check(bool valid) { if (!valid) throw new Exception("camera yaw regression"); }
}
