using System;
using PeakCraft.Hazards;
using UnityEngine;

static class Program
{
    private static int passed;
    static void Main()
    {
        Test("held native contact bodies stay dynamic and awake without moving", () =>
        {
            var (character, part, rig) = Body();
            rig.linearVelocity = new(4, 5, 6); rig.angularVelocity = new(1, 2, 3);
            var held = new NativeContactLock(); held.Take(character);
            Check(held.Count == 1 && !rig.isKinematic && !rig.useGravity);
            Check(rig.constraints == RigidbodyConstraints.FreezeAll && rig.sleepThreshold == 0);
            Check(rig.linearVelocity == Vector3.zero && rig.angularVelocity == Vector3.zero);
            int awake = rig.WakeCount; held.Wake(); Check(rig.WakeCount == awake + 1);
        });
        Test("handoff restores each body's original constraints gravity and sleeping", () =>
        {
            var (character, _, rig) = Body();
            rig.constraints = RigidbodyConstraints.FreezeRotationY; rig.useGravity = false; rig.sleepThreshold = .125f;
            var second = new Rigidbody { constraints = RigidbodyConstraints.FreezePositionX, sleepThreshold = .75f };
            character.refs.ragdoll.partList.Add(Part(second));
            var held = new NativeContactLock(); held.Take(character); held.Release();
            Check(held.Count == 0 && rig.constraints == RigidbodyConstraints.FreezeRotationY && !rig.useGravity && rig.sleepThreshold == .125f);
            Check(second.constraints == RigidbodyConstraints.FreezePositionX && second.useGravity && second.sleepThreshold == .75f);
        });
        Test("hazard force queues cannot accumulate during Minecraft control", () =>
        {
            var (character, part, _) = Body();
            part.forcesToAdd = new(0, 100, 0);
            var held = new NativeContactLock(); held.Take(character); Check(part.forcesToAdd == Vector3.zero);
            for (int frame = 0; frame < 20; frame++)
            {
                part.forcesToAdd = new(300, 400, 500); held.Wake(); Check(part.forcesToAdd == Vector3.zero);
            }
            part.forcesToAdd = new(0, 1000, 0); held.Release(); Check(part.forcesToAdd == Vector3.zero);
        });
        Test("repeated take cannot replace saved native settings with FreezeAll", () =>
        {
            var (character, _, rig) = Body();
            rig.constraints = RigidbodyConstraints.FreezePositionX;
            var held = new NativeContactLock(); held.Take(character); held.Take(character); held.Release();
            Check(rig.constraints == RigidbodyConstraints.FreezePositionX && rig.useGravity && rig.sleepThreshold == .005f);
        });
        Test("taking another scout first restores the previous scout", () =>
        {
            var (first, firstPart, firstRig) = Body();
            var (second, _, secondRig) = Body();
            var held = new NativeContactLock(); held.Take(first); firstPart.forcesToAdd = new(0, 90, 0); held.Take(second);
            Check(firstRig.constraints == RigidbodyConstraints.None && firstRig.useGravity && firstPart.forcesToAdd == Vector3.zero);
            Check(secondRig.constraints == RigidbodyConstraints.FreezeAll && held.Count == 1);
            held.Release(); Check(secondRig.constraints == RigidbodyConstraints.None && secondRig.useGravity);
        });
        Test("colliderless and missing bodies remain outside the native contact lock", () =>
        {
            var character = new Character(); var untouched = new Rigidbody { isKinematic = true, sleepThreshold = .3f };
            character.refs.ragdoll.partList.Add(new Bodypart { Rig = untouched });
            character.refs.ragdoll.partList.Add(Part(null));
            var held = new NativeContactLock(); held.Take(character); held.Wake(); held.Release();
            Check(held.Count == 0 && untouched.isKinematic && untouched.useGravity && untouched.sleepThreshold == .3f && untouched.WakeCount == 0);
        });
        Test("duplicate body references do not freeze a saved snapshot twice", () =>
        {
            var (character, part, rig) = Body(); character.refs.ragdoll.partList.Add(part);
            var held = new NativeContactLock(); held.Take(character); Check(held.Count == 1);
            held.Release(); held.Release(); held.Wake(); Check(rig.constraints == RigidbodyConstraints.None && rig.useGravity);
        });
        Console.WriteLine($"PASS: {passed} native contact lock lifecycle tests");
    }
    static (Character, Bodypart, Rigidbody) Body()
    {
        var rig = new Rigidbody(); var part = Part(rig); var character = new Character();
        character.refs.ragdoll.partList.Add(part); return (character, part, rig);
    }
    static Bodypart Part(Rigidbody? rig) { var part = new Bodypart { Rig = rig! }; part.colliders.Add(new object()); return part; }
    static void Test(string name, Action run) { run(); passed++; Console.WriteLine("PASS: " + name); }
    static void Check(bool valid) { if (!valid) throw new Exception("Native contact handoff regression"); }
}
