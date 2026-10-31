using System;
using BepInEx.Configuration;
using PeakCraft;
using PeakCraft.Player;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class NativeClimbFlowTests
{
    public static void Run(Action<string, Action> test)
    {
        test("real helper keeps native ownership after ledge detach, then returns once settled", () =>
        {
            var (climb, c) = Start();
            c.data.isClimbing = false;
            At(1); climb.TickNative(c);
            Check(climb.Requested && climb.WantsNative);
            c.data.isGrounded = true;
            At(1.02f); climb.TickNative(c);
            Check(climb.Requested);
            At(1.34f); climb.TickNative(c);
            Check(!climb.Requested && climb.WantsNative);
            Check(!climb.ConsumeReturnTeleport());
            At(1.36f); climb.UpdateRequest(true, false, c);
            Check(!climb.WantsNative && climb.ConsumeReturnTeleport());
            climb.UpdateRequest(true, false, c);
            Check(!climb.Requested); // V must be released before another request.
        });
        test("exhausted airborne detach does not call the helper grab repeatedly", () =>
        {
            var (climb, c) = Start();
            c.data.isClimbing = false; c.data.currentStamina = 0;
            c.data.avarageVelocity = new Vector3 { y = -4 };
            int attempts = c.refs.climbing.Attempts;
            At(1); climb.TickNative(c); At(10); climb.TickNative(c);
            Check(c.refs.climbing.Attempts == attempts && climb.WantsNative);
        });
        test("explicit V release still stops the climb and returns after a physics tick", () =>
        {
            var (climb, c) = Start();
            Keyboard.current[Key.V].isPressed = false;
            At(1); climb.UpdateRequest(true, false, c);
            Check(c.refs.climbing.Stops == 1 && !climb.Requested && climb.WantsNative);
            var input = new CharacterInput(); climb.ApplyInput(input);
            Check(!input.usePrimaryIsPressed && input.usePrimaryWasReleased);
            At(1.02f); climb.UpdateRequest(true, false, c);
            Check(!climb.WantsNative);
        });
        test("Space uses the native release input and does not wait for a ledge landing", () =>
        {
            var (climb, c) = Start();
            var input = new CharacterInput { jumpWasPressed = true };
            climb.ApplyInput(input);
            Check(input.jumpWasPressed); // Native game must still receive the event.
            c.data.isClimbing = false; // Native CharacterClimbing processes Space.
            At(1); climb.TickNative(c);
            Check(!climb.Requested && c.refs.climbing.Stops == 0);
            At(1.02f); climb.UpdateRequest(true, false, c);
            Check(!climb.WantsNative);
        });
        test("opening a menu cancels a virtual climb", () =>
        {
            var (climb, c) = Start();
            At(1); climb.UpdateRequest(true, true, c);
            Check(!climb.Requested && c.refs.climbing.Stops == 1);
        });
        test("ending Minecraft preference releases only helper-originated native grabs", () =>
        {
            var (climb, c) = Start();
            climb.UpdateRequest(false, false, c);
            Check(!climb.WantsNative && c.refs.climbing.Stops == 1);
            (climb, c) = Start(preexisting: true);
            climb.UpdateRequest(false, false, c);
            Check(!climb.WantsNative && c.refs.climbing.Stops == 0);
        });
        test("a physically held native grab survives a preference change", () =>
        {
            var (climb, c) = Start();
            CharacterInput.action_usePrimary.Held = true;
            climb.UpdateRequest(false, false, c);
            Check(!climb.WantsNative && c.refs.climbing.Stops == 0);
        });
        test("death, passout and loading clear a pending ledge handoff", () =>
        {
            foreach (int condition in new[] { 0, 1, 2 })
            {
                var (climb, c) = Start();
                c.data.isClimbing = false;
                At(1); climb.TickNative(c);
                if (condition == 0) c.data.dead = true;
                if (condition == 1) c.data.passedOut = true;
                if (condition == 2) LoadingScreenHandler.loading = true;
                climb.UpdateRequest(true, false, c);
                Check(!climb.Requested && !climb.WantsNative && !climb.Active);
            }
        });
    }

    private static (NativeClimb climb, Character c) Start(bool preexisting = false)
    {
        At(0); Application.isFocused = true; LoadingScreenHandler.loading = false;
        Keyboard.current = new(); Keyboard.current[Key.V].isPressed = true;
        CharacterInput.action_usePrimary = new();
        Plugin.Instance = new();
        var c = new Character(); c.data.isClimbing = preexisting;
        var climb = new NativeClimb(new ConfigFile());
        climb.UpdateRequest(true, false, c);
        climb.TickNative(c);
        var input = new CharacterInput(); climb.ApplyInput(input);
        Check(climb.Requested && climb.Active && c.data.isClimbing);
        return (climb, c);
    }
    private static void At(float value) { Time.unscaledTime = Time.fixedTime = value; }
    private static void Check(bool value) { if (!value) throw new Exception("native climb request/return flow regression"); }
}
