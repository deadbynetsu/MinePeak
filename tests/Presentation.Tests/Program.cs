using System;
using PeakCraft.Player;

internal static class Program
{
    private static int passed;

    private static void Main()
    {
        Test("native death does not inherit the old inclusive owner-range overlay bug", () =>
        {
            // Cutscene was numerically inside the old MinecraftOwns..Cutscene
            // range. Establish that this scenario catches that original mistake.
            Check((uint)((int)Owner.Cutscene - 2) <= 2u, "original range reproduction");
            Expect(E(Owner.Cutscene, incapacitated: true), false, false, true, true);
        });
        Test("ordinary Minecraft control presents HUD and models while keeping placed blocks", () =>
            Expect(E(Owner.MinecraftOwns), true, true, true, false));
        Test("native control never displays the Minecraft first-person image", () =>
            Expect(E(Owner.NativeControl), false, false, true, false));
        Test("native PEAK ownership has no stale Minecraft models", () =>
            Expect(E(Owner.PeakOwns), false, false, true, false));
        Test("handoff waits before presenting a potentially stale Minecraft image", () =>
            Expect(E(Owner.Handoff), false, false, true, false));
        Test("Minecraft-owned menus retain their existing presentation eligibility", () =>
            Expect(E(Owner.PeakMenu), true, true, true, false));
        Test("ordinary native cutscenes keep placed blocks without the Minecraft image", () =>
            Expect(E(Owner.Cutscene), false, false, true, false));
        Test("disconnected Java cannot leave any Minecraft presentation behind", () =>
        {
            foreach (Owner owner in Enum.GetValues<Owner>())
                Expect(E(owner, linked: false, incapacitated: true), false, false, false, false);
        });
        Test("native viewer preference preserves the scout even when incapacitated", () =>
        {
            foreach (Owner owner in Enum.GetValues<Owner>())
                Expect(E(owner, minecraftMode: false, incapacitated: true), false, false, true, false);
        });
        Test("a missing character cannot retain a HUD body override or world placement", () =>
        {
            foreach (Owner owner in Enum.GetValues<Owner>())
                Expect(E(owner, characterPresent: false, incapacitated: true), false, false, false, false);
        });
        Test("loading hides world meshes and restores visual body ownership", () =>
        {
            foreach (Owner owner in Enum.GetValues<Owner>())
                Expect(E(owner, loading: true, incapacitated: true), false, false, false, false);
        });
        Test("a lethal event suppresses the HUD before the ownership update catches up", () =>
        {
            // Vitals can make the native body incapacitated after Frame decided
            // MinecraftOwns. A state-only guard would leak a last hand/HUD frame.
            Expect(E(Owner.MinecraftOwns, incapacitated: true), false, false, true, true);
        });
        Test("incapacitation during native equipment control also hides the local scout", () =>
            Expect(E(Owner.NativeControl, incapacitated: true), false, false, true, true));
        Test("incapacitation while a menu was open cannot retain the Minecraft screen", () =>
            Expect(E(Owner.PeakMenu, incapacitated: true), false, false, true, true));
        Test("the native end screen suppresses HUD and models even before a state transition", () =>
        {
            foreach (Owner owner in Enum.GetValues<Owner>())
                Expect(E(owner, endScreen: true), false, false, true, false);
        });
        Test("an incapacitated Minecraft scout stays hidden under the native end screen", () =>
            Expect(E(Owner.Cutscene, incapacitated: true, endScreen: true), false, false, true, true));
        Test("revival releases the body override before the next Minecraft handoff", () =>
        {
            Expect(E(Owner.Cutscene, incapacitated: true), false, false, true, true);
            Expect(E(Owner.Cutscene), false, false, true, false);
            Expect(E(Owner.Handoff), false, false, true, false);
            Expect(E(Owner.MinecraftOwns), true, true, true, false);
        });
        Test("switching to PEAK while dead restores the local body without hiding placed blocks", () =>
        {
            Expect(E(Owner.Cutscene, incapacitated: true), false, false, true, true);
            Expect(E(Owner.Cutscene, incapacitated: true, minecraftMode: false), false, false, true, false);
        });
        Test("losing and recovering the Java link during death never exposes a stale HUD", () =>
        {
            Expect(E(Owner.Cutscene, incapacitated: true), false, false, true, true);
            Expect(E(Owner.PeakOwns, linked: false, incapacitated: true), false, false, false, false);
            Expect(E(Owner.PeakOwns, incapacitated: true), false, false, true, true);
            Expect(E(Owner.Cutscene, incapacitated: true), false, false, true, true);
        });
        Test("placed structures remain visible through the entire death and revival sequence", () =>
        {
            var sequence = new[] {
                E(Owner.MinecraftOwns),
                E(Owner.MinecraftOwns, incapacitated: true),
                E(Owner.Cutscene, incapacitated: true),
                E(Owner.Cutscene, incapacitated: true, endScreen: true),
                E(Owner.Cutscene), E(Owner.Handoff), E(Owner.MinecraftOwns)
            };
            foreach (PresentationState state in sequence)
                Check(state.PlacedBlocks, "a placed bridge disappeared during death or revival");
        });
        BodyVisibilityTests.Run();
        CorpseCacheTests.Run();
        Console.WriteLine($"PASS: {passed} presentation policy and lifecycle tests");
    }

    private static PresentationState E(Owner owner, bool linked = true, bool minecraftMode = true,
        bool characterPresent = true, bool incapacitated = false, bool loading = false, bool endScreen = false)
        => PresentationPolicy.Evaluate(owner, linked, minecraftMode, characterPresent, incapacitated, loading, endScreen);

    internal static void Expect(PresentationState state, bool overlay, bool models, bool blocks, bool hidden)
    {
        Check(state.Overlay == overlay, $"overlay expected {overlay}, observed {state.Overlay}");
        Check(state.MinecraftModels == models, $"models expected {models}, observed {state.MinecraftModels}");
        Check(state.PlacedBlocks == blocks, $"placed blocks expected {blocks}, observed {state.PlacedBlocks}");
        Check(state.HideNativeBody == hidden, $"native body override expected {hidden}, observed {state.HideNativeBody}");
    }

    internal static void Test(string name, Action body)
    {
        body(); passed++; Console.WriteLine("PASS: " + name);
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Presentation regression: " + message);
    }
}
