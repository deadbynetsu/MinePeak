namespace PeakCraft.Player;

internal readonly record struct PresentationState(
    bool Overlay, bool MinecraftModels, bool PlacedBlocks, bool HideNativeBody);

/// <summary>Presentation is independent of who simulates the native body.</summary>
internal static class PresentationPolicy
{
    public static PresentationState Evaluate(Owner owner, bool linked, bool minecraftMode,
        bool characterPresent, bool incapacitated, bool loading, bool endScreen)
    {
        bool scene = linked && characterPresent && !loading;
        bool minecraft = scene && minecraftMode && !incapacitated && !endScreen &&
            (owner == Owner.MinecraftOwns || owner == Owner.PeakMenu);
        return new(minecraft, minecraft, scene,
            scene && minecraftMode && incapacitated);
    }
}
