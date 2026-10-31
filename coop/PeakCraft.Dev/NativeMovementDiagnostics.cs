using System;
using System.Reflection;
using Peak;
using Peak.Afflictions;
using UnityEngine;
using Zorro.Core;

namespace PeakCraft.Dev;

/// <summary>Read-only, opt-in diagnostics for the real native movement effects.</summary>
internal static class NativeMovementDiagnostics
{
    public static void Describe(Character c)
    {
        if (c == null) { Plugin.Log.LogInfo("movementstatus: no local character"); return; }
        var d = c.data;
        var movement = c.refs.movement;
        for (byte slot = 0; slot < 4; slot++)
        {
            var item = c.player.GetItemSlot(slot);
            Plugin.Log.LogInfo($"movementstatus: pocket {slot + 1} {(item == null || item.IsEmpty() ? "empty" : item.GetPrefabName())}");
        }
        Plugin.Log.LogInfo($"movementstatus: owner {Plugin.Instance.Ownership.State}; helper {Plugin.Instance.Jump.WantsNative}; grounded {d.isGrounded}; groundedFor {d.groundedFor:F3}; sinceGrounded {d.sinceGrounded:F3}; charging {d.chargingJump}; jumping {d.isJumping}; sinceJump {d.sinceJump:F3}; velocity {d.avarageVelocity:F3}");
        Plugin.Log.LogInfo($"movementstatus: extra available {d.extraJumps}; passive total {c.refs.afflictions.totalExtraJumps}; ordinary jumps {d.jumpsRemaining}; petrify {d.petrifyAmount}; stamina {d.currentStamina:F3}; item {(d.currentItem != null ? d.currentItem.name : "none")}");
        Plugin.Log.LogInfo($"movementstatus: native jumpImpulse {movement.jumpImpulse:F3}; gravity {movement.jumpGravity:F3}; maxGravity {movement.maxGravity:F3}; balloonJumpMultiplier {movement.balloonJumpMultiplier:F3}; balloonFloatMultiplier {movement.balloonFloatMultiplier:F3}");
        foreach (Affliction effect in c.refs.afflictions.afflictionList)
            Plugin.Log.LogInfo("movementstatus: applied " + Effect(effect));

        foreach (var entry in SingletonAsset<ItemDatabase>.Instance.itemLookup)
        {
            var prefab = entry.Value;
            var action = prefab.GetComponentInChildren<Action_SuperJumpAmulet>(true);
            var passive = prefab.GetComponentInChildren<DoubleJumpAmulet>(true);
            if (action == null && passive == null) continue;
            Plugin.Log.LogInfo($"movementstatus: prefab {entry.Key} '{prefab.name}' doubleJumpComponent {passive != null}; superJumpAction {action != null}");
            if (action != null) DescribeAction("prefab", action);
            if (passive != null) Plugin.Log.LogInfo($"movementstatus: passive startActive {passive.startActive}; petrifyPerJump {passive.petrifyPerJump}");
        }

        int count = 0;
        foreach (var action in UnityEngine.Object.FindObjectsByType<Action_SuperJumpAmulet>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(action.transform.position, c.Center) > 20f) continue;
            DescribeAction($"nearby {action.name} active {action.isActiveAndEnabled}", action);
            if (++count >= 8) break;
        }
    }

    private static void DescribeAction(string where, Action_SuperJumpAmulet action)
    {
        Plugin.Log.LogInfo($"movementstatus: {where} action petrifyPerUse {action.petrifyPerUse:F4}; affliction {Effect(action.affliction)}");
        if (action.extraAfflictions != null)
            foreach (Affliction effect in action.extraAfflictions)
                Plugin.Log.LogInfo("movementstatus: extra affliction " + Effect(effect));
    }

    private static string Effect(Affliction? effect)
    {
        if (effect == null) return "none";
        string text = effect.GetType().FullName ?? effect.GetType().Name;
        foreach (string name in new[] { "stacks", "radius", "forceAmt", "forceTime", "lowGravAmount", "lowGravTime", "totalTime", "duration", "gravityMultiplier" })
        {
            FieldInfo? field = effect.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) continue;
            object? value = field.GetValue(effect);
            if (value is IFormattable scalar)
                text += $"; {name} {scalar.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}";
        }
        return text;
    }
}
