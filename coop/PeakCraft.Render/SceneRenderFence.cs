namespace PeakCraft.Render;

/// <summary>
/// Geometry messages have no scene ID. After a scene switch, discard the old ring backlog
/// until the producer's ordered readiness marker identifies this scene and collision epoch.
/// Texture messages remain usable across worlds. Accepting a marker must clear scene meshes
/// before the caller drains the following geometry messages.
/// </summary>
internal sealed class SceneRenderFence
{
    private uint acceptedWorld;
    private uint acceptedEpoch;

    public bool Ready { get; private set; }

    public void RequireScene()
    {
        Ready = false;
        acceptedWorld = acceptedEpoch = 0;
    }

    /// <returns>True only when a valid new readiness boundary was accepted.</returns>
    public bool AcceptReady(uint world, uint epoch, uint expectedWorld, uint expectedEpoch)
    {
        if (world == 0 || world != expectedWorld || epoch != expectedEpoch) return false;
        if (Ready && acceptedWorld == world && acceptedEpoch == epoch) return false;
        acceptedWorld = world;
        acceptedEpoch = epoch;
        Ready = true;
        return true;
    }

    public bool Allow(uint type)
    {
        if (type == 13) return false; // the caller validates the marker separately
        return Ready || (type != 2 && type != 3 && type != 5 && type != 6 && type != 9 && type != 12);
    }
}
