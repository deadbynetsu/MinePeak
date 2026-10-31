using PeakCraft.Player;
using UnityEngine;

namespace PeakCraft.Dev;

/// <summary>Explicit harness-only fixtures using unmodified native hazard components.</summary>
internal static class HazardTestScene
{
    private static GameObject? fixture;

    public static void Change(string kind)
    {
        if (fixture != null) Object.Destroy(fixture);
        fixture = null;
        if (kind == "clear")
        {
            Plugin.Instance.Collision.Reset("native hazard test removed");
            Plugin.Log.LogInfo("hazardtest: fixture removed");
            return;
        }
        if (kind != "collision" && kind != "trigger")
            throw new System.ArgumentException("hazardtest: collision, trigger or clear");
        var character = Character.localCharacter;
        if (character == null) return;
        var forward = Vector3.ProjectOnPlane(character.data.lookDirection, Vector3.up).normalized;
        if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
        fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.SetActive(false);
        fixture.name = "MinePeak harness native poison " + kind;
        fixture.layer = LayerMask.NameToLayer("Map");
        fixture.transform.position = Follower.PeakFeet(character) + forward * 1.5f + Vector3.up;
        fixture.transform.rotation = Quaternion.LookRotation(forward);
        fixture.transform.localScale = new Vector3(1.5f, 2f, .3f);
        if (kind == "collision")
        {
            var modifier = fixture.AddComponent<CollisionModifier>();
            modifier.damage = .1f;
            modifier.cooldown = 1f;
            modifier.statusType = CharacterAfflictions.STATUSTYPE.Poison;
            modifier.knockback = 0;
            float lastLog = -10;
            int contacts = 0;
            modifier.onCollide += (c, m, collision, part) =>
            {
                if (c != Character.localCharacter) return;
                contacts++;
                if (Time.unscaledTime - lastLog < .5f) return;
                lastLog = Time.unscaledTime;
                Plugin.Log.LogInfo($"hazardtest: real native contacts {contacts}; collision valid {collision != null}; points {collision?.contactCount ?? 0}; body {part?.partType}; poison before {c.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Poison):F4}");
            };
        }
        else
        {
            fixture.GetComponent<BoxCollider>().isTrigger = true;
            var trigger = fixture.AddComponent<StatusTrigger>();
            trigger.cooldown = 1f;
            trigger.addStatus = true;
            trigger.statusType = CharacterAfflictions.STATUSTYPE.Poison;
            trigger.statusAmount = .1f;
        }
        // No persistent objects: Unity also removes this fixture on scene exit/quit.
        fixture.SetActive(true);
        Physics.SyncTransforms();
        Plugin.Instance.Collision.Reset("native hazard test " + kind);
        Plugin.Log.LogInfo($"hazardtest: real {kind} component; center {fixture.transform.position:F3}; forward {forward:F3}; poison 0.1; native cooldown 1s; knockback 0");
    }
}
