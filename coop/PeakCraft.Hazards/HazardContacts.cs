using System.Collections.Generic;
using PeakCraft.Player;
using UnityEngine;

namespace PeakCraft.Hazards;

/// <summary>Opt-in diagnostics of real PEAK hazard components and body contacts.</summary>
internal static class HazardContacts
{
    public static void Describe(float radius = 6f)
    {
        var character = Character.localCharacter;
        if (character == null) { Plugin.Log.LogInfo("hazardcontacts: no character"); return; }
        radius = Mathf.Clamp(radius, .1f, 200f);
        Physics.SyncTransforms();
        var body = new List<Collider>();
        foreach (var part in character.refs.ragdoll.partList)
            foreach (var entry in part.colliders)
            {
                var collider = entry.Col();
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy) body.Add(collider);
            }

        var feet = Follower.PeakFeet(character);
        int dynamicBodies = 0, frozenBodies = 0;
        foreach (var part in character.refs.ragdoll.partList)
            if (part.Rig != null && !part.Rig.isKinematic)
            { dynamicBodies++; if (part.Rig.constraints == RigidbodyConstraints.FreezeAll) frozenBodies++; }
        Plugin.Log.LogInfo($"hazardcontacts: owner {Plugin.Instance.Ownership.State}; feet {feet:F3}; native body colliders {body.Count}; kinematic {character.data.isKinecmatic}; dynamic bodies {dynamicBodies}; frozen bodies {frozenBodies}; radius {radius:F2}");
        Plugin.Log.LogInfo($"hazardcontacts: native filters airport {character.refs.afflictions.m_inAirport}; statuses locked {character.statusesLocked}; invincible {character.data.isInvincible}; skeleton {character.data.isSkeleton}; zombie {character.isZombie}; scoutmaster {character.isScoutmaster}; local {character.IsLocal}; mine {character.photonView.IsMine}");
        var statuses = character.refs.afflictions.currentStatuses;
        for (int i = 0; i < statuses.Length; i++)
            if (statuses[i] != 0f)
                Plugin.Log.LogInfo($"hazardcontacts: status {(CharacterAfflictions.STATUSTYPE)i} {statuses[i]:F4}");

        int found = 0;
        foreach (var collider in Physics.OverlapSphere(character.Center, radius, ~0, QueryTriggerInteraction.Collide))
        {
            if (CharacterRagdoll.TryGetCharacterFromCollider(collider, out var owner) && owner == character) continue;
            var modifier = collider.GetComponentInParent<CollisionModifier>();
            var trigger = collider.GetComponentInParent<StatusTrigger>();
            if (modifier == null && trigger == null) continue;
            found++;
            int overlaps = 0;
            float depth = 0;
            foreach (var capsule in body)
                if (Physics.ComputePenetration(capsule, capsule.transform.position, capsule.transform.rotation,
                    collider, collider.transform.position, collider.transform.rotation, out _, out var distance))
                { overlaps++; depth = Mathf.Max(depth, distance); }
            float gap = Vector3.Distance(collider.ClosestPoint(character.Center), character.Center);
            var rig = collider.attachedRigidbody;
            Plugin.Log.LogInfo($"hazardcontacts: collider '{collider.name}' layer {LayerMask.LayerToName(collider.gameObject.layer)} trigger {collider.isTrigger} dynamic {(rig != null && !rig.isKinematic)} center gap {gap:F3} body overlaps {overlaps} depth {depth:F3}");
            if (modifier != null)
                Plugin.Log.LogInfo($"hazardcontacts: modifier '{modifier.name}' same object {modifier.gameObject == collider.gameObject} active {modifier.isActiveAndEnabled} effects {modifier.applyEffects} damage {modifier.damage:F4} type {modifier.statusType} cooldown {modifier.cooldown:F3} knockback {modifier.knockback:F3} petrify {modifier.applyPetrify} additional {modifier.additionalMods.Count} callbacks {(modifier.onCollide?.GetInvocationList().Length ?? 0)}");
            if (trigger != null)
                Plugin.Log.LogInfo($"hazardcontacts: trigger '{trigger.name}' active {trigger.isActiveAndEnabled} status {trigger.addStatus} type {trigger.statusType} amount {trigger.statusAmount:F4} cooldown {trigger.cooldown:F3} poison over time {trigger.poisonOverTime} duration {trigger.poisonOverTimeDuration:F3} delay {trigger.poisonOverTimeDelay:F3} per second {trigger.poisonOverTimeAmountPerSecond:F4}");
        }
        Plugin.Log.LogInfo($"hazardcontacts: {found} nearby hazard collider(s)");
    }
}
