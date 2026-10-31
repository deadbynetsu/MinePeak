using System.Collections.Generic;

// A small boundary for the lifecycle tests. Unity itself is exercised in the
// opt-in in-game contact fixture; these types cannot simulate its collision solver.
namespace UnityEngine
{
    internal readonly record struct Vector3(float x, float y, float z)
    {
        public static Vector3 zero => default;
    }
    [System.Flags]
    internal enum RigidbodyConstraints
    {
        None = 0, FreezePositionX = 2, FreezeRotationY = 32, FreezeAll = 126
    }
    internal sealed class Rigidbody
    {
        public bool useGravity = true, isKinematic = true;
        public RigidbodyConstraints constraints;
        public float sleepThreshold = .005f;
        public Vector3 linearVelocity, angularVelocity;
        public int WakeCount;
        public void WakeUp() => WakeCount++;
    }
}

internal sealed class Bodypart
{
    public UnityEngine.Rigidbody Rig = null!;
    public UnityEngine.Vector3 forcesToAdd;
    public readonly List<object> colliders = new();
}
internal sealed class Character
{
    public readonly References refs = new();
    internal sealed class References { public readonly Ragdoll ragdoll = new(); }
    internal sealed class Ragdoll { public readonly List<Bodypart> partList = new(); }
}
