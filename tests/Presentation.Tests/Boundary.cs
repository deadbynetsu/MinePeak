using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// This boundary represents renderer ownership and Unity's destroyed-object null
// comparisons. It deliberately exposes no Rigidbody, physics, damage, camera,
// or character movement API: the linked production hider can only change visuals.
namespace UnityEngine
{
    internal class Object
    {
        public bool Destroyed;
        private static bool IsNull(Object? value) => ReferenceEquals(value, null) || value.Destroyed;
        public static bool operator ==(Object? left, Object? right)
            => IsNull(left) ? IsNull(right) : !IsNull(right) && ReferenceEquals(left, right);
        public static bool operator !=(Object? left, Object? right) => !(left == right);
        public override bool Equals(object? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }

    internal sealed class Renderer : Object
    {
        private bool forced;
        public bool enabled = true;
        public int ForceReads { get; private set; }
        public int ForceWrites { get; private set; }
        public bool forceRenderingOff
        {
            get { RequireAlive(); ForceReads++; return forced; }
            set { RequireAlive(); ForceWrites++; forced = value; }
        }
        private void RequireAlive()
        {
            if (Destroyed) throw new InvalidOperationException("Destroyed Unity renderer was accessed");
        }
    }
}

internal sealed class Character : UnityEngine.Object
{
    public readonly List<UnityEngine.Renderer> Renderers = new();
    public int HierarchyReads { get; private set; }
    public T[] GetComponentsInChildren<T>(bool includeInactive)
    {
        if (Destroyed) throw new InvalidOperationException("Destroyed Unity character was accessed");
        if (!includeInactive) throw new InvalidOperationException("Hidden native parts must also be captured");
        HierarchyReads++;
        return Renderers.Cast<T>().ToArray();
    }
}

namespace PeakCraft
{
    internal static class Plugin
    {
        public static readonly Logger Log = new();
        internal sealed class Logger { public void LogInfo(string value) { } }
    }
}
