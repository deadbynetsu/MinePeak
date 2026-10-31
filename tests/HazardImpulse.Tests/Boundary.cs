using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

// These boundaries exercise the patch's call-site and forwarding contracts.
// Genuine contact generation, cooldowns and movement are tested inside PEAK.
namespace UnityEngine
{
    internal readonly record struct Vector3(float x, float y, float z)
    {
        public float sqrMagnitude => x*x+y*y+z*z;
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] internal sealed class HarmonyPatch : Attribute { }
    internal static class AccessTools
    {
        public static MethodInfo Method(Type type, string name, Type[]? arguments = null)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            return (arguments == null ? type.GetMethod(name, flags) : type.GetMethod(name, flags, arguments))
                ?? throw new MissingMethodException(type.FullName, name);
        }
    }
    internal sealed class CodeInstruction
    {
        public OpCode opcode;
        public object operand;
        public readonly List<string> labels = new(), blocks = new();
        public CodeInstruction(OpCode op, object target) { opcode = op; operand = target; }
        public bool Calls(MethodInfo method) => (opcode == OpCodes.Call || opcode == OpCodes.Callvirt) && Equals(operand, method);
    }
}
namespace PeakCraft
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public int Warnings;
        public void LogInfo(string value) { }
        public void LogWarning(string value) => Warnings++;
    }
}
namespace PeakCraft.Patches
{
    internal static class NativeFlightPatch
    {
        public static Action<CharacterMovement, float>? OnRetain;
        public static void Retain(CharacterMovement movement, float seconds) => OnRetain?.Invoke(movement, seconds);
    }
}
internal sealed class CharacterMovement { }
internal sealed class CharacterRefs { public readonly CharacterMovement movement = new(); }
internal sealed class Character
{
    public readonly CharacterRefs refs = new();
    public readonly List<string> Events = new();
    public int Calls;
    public UnityEngine.Vector3 Force;
    public float Min, Max;
    public void AddForce(UnityEngine.Vector3 force, float min = 1, float max = 1)
    {
        Events.Add("native force"); Calls++; Force=force; Min=min; Max=max;
    }
    public void AddForce(object other) => throw new InvalidOperationException("unrelated overload");
}
internal sealed class CollisionModifier
{
    public void Collide(Character character)
    {
        character.AddForce(default(UnityEngine.Vector3));
        _=BounceRoutine();
        IEnumerator BounceRoutine() { character.AddForce(default(UnityEngine.Vector3)); yield return null; }
    }
    public IEnumerator Unrelated() { yield return null; }
}
