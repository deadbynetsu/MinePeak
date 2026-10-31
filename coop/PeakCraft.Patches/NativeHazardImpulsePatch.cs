using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace PeakCraft.Patches;

/// <summary>
/// Keep the local body native only when a real collision modifier reaches its
/// own force call. Native cooldowns, immunity decisions and bounce conditions
/// have already run; posture, gravity and ordinary ground contacts are untouched.
/// </summary>
[HarmonyPatch]
internal static class NativeHazardImpulsePatch
{
    private static readonly MethodInfo OriginalForce = AccessTools.Method(typeof(Character), "AddForce",
        new[] { typeof(Vector3), typeof(float), typeof(float) });
    private static readonly MethodInfo BridgedForce = AccessTools.Method(typeof(NativeHazardImpulsePatch), nameof(ApplyForce));

    private static IEnumerable<MethodBase> TargetMethods()
    {
        var targets = new List<MethodBase> {
            AccessTools.Method(typeof(CollisionModifier), "Collide")
        };
        FindBounceRoutine(typeof(CollisionModifier), targets);
        // Refuse a changed native method layout instead of silently omitting
        // the coroutine's first force and claiming that bounces are covered.
        if (targets.Count != 2)
            throw new InvalidOperationException("Native collision bounce routine layout is unsupported.");
        return targets;
    }

    private static void FindBounceRoutine(Type owner, List<MethodBase> targets)
    {
        foreach (Type nested in owner.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (nested.Name.IndexOf("BounceRoutine", StringComparison.Ordinal) >= 0 &&
                typeof(IEnumerator).IsAssignableFrom(nested))
                targets.Add(AccessTools.Method(nested, "MoveNext"));
            FindBounceRoutine(nested, targets);
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var result = new List<CodeInstruction>(instructions);
        int changed = 0;
        foreach (var instruction in result)
            if (instruction.Calls(OriginalForce))
            {
                // The static wrapper consumes the exact same stack values as
                // the instance call. Mutating this instruction keeps its labels
                // and exception-block boundaries in place.
                instruction.opcode = OpCodes.Call;
                instruction.operand = BridgedForce;
                changed++;
            }
        if (changed == 0)
            throw new InvalidOperationException("Native collision force call site was not found in " + __originalMethod.Name + ".");
        Plugin.Log.LogInfo("hazards: native impulse bridge wired in " + __originalMethod.Name + " (" + changed + " call site(s))");
        return result;
    }

    private static void ApplyForce(Character character, Vector3 force, float minRandomMultiplier, float maxRandomMultiplier)
    {
        try
        {
            // A zero/invalid vector cannot initiate native motion. The original
            // call still receives it unchanged, preserving the game's behavior.
            float magnitude = force.sqrMagnitude;
            if (magnitude > 0f && !float.IsInfinity(magnitude) && !float.IsNaN(magnitude))
                NativeFlightPatch.Retain(character.refs.movement, 0f);
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning("hazards: native impulse retention failed: " + error.Message);
        }
        character.AddForce(force, minRandomMultiplier, maxRandomMultiplier);
    }
}
