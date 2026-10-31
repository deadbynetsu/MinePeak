using System.Collections.Generic;
using UnityEngine;

namespace PeakCraft.Hazards;

/// <summary>
/// Holds the local scout still without suppressing Unity's real contacts with
/// static hazards. Kinematic/static pairs do not deliver PEAK's collision damage.
/// </summary>
internal sealed class NativeContactLock
{
    private readonly List<State> held = new List<State>();

    public int Count => held.Count;

    public void Take(Character character)
    {
        Release();
        foreach (var part in character.refs.ragdoll.partList)
        {
            var rig = part.Rig;
            if (rig == null || part.colliders.Count == 0 || held.Exists(state => state.Rig == rig)) continue;
            held.Add(new State(part));
            part.forcesToAdd = Vector3.zero;
            rig.useGravity = false;
            rig.constraints = RigidbodyConstraints.FreezeAll;
            rig.sleepThreshold = 0f;
            rig.isKinematic = false;
            rig.linearVelocity = Vector3.zero;
            rig.angularVelocity = Vector3.zero;
            rig.WakeUp();
        }
    }

    public void Wake()
    {
        foreach (var state in held)
            if (state.Rig != null)
            {
                // Native movement is suspended while following Minecraft. Its
                // force accumulator must not build up for the later handoff.
                state.Part.forcesToAdd = Vector3.zero;
                state.Rig.WakeUp();
            }
    }

    public void Release()
    {
        foreach (var state in held)
        {
            var rig = state.Rig;
            if (rig == null) continue;
            state.Part.forcesToAdd = Vector3.zero;
            rig.constraints = state.Constraints;
            rig.useGravity = state.Gravity;
            rig.sleepThreshold = state.SleepThreshold;
        }
        held.Clear();
    }

    private readonly struct State
    {
        public readonly Rigidbody Rig;
        public readonly Bodypart Part;
        public readonly RigidbodyConstraints Constraints;
        public readonly bool Gravity;
        public readonly float SleepThreshold;

        public State(Bodypart part)
        {
            Part = part;
            var rig = part.Rig;
            Rig = rig;
            Constraints = rig.constraints;
            Gravity = rig.useGravity;
            SleepThreshold = rig.sleepThreshold;
        }
    }
}
