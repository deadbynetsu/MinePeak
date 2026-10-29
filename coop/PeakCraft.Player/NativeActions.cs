using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using Zorro.Core;

namespace PeakCraft.Player;

/// <summary>Temporary original PEAK equipment controls inside the Minecraft preference.</summary>
internal sealed class NativeActions
{
    private readonly ConfigEntry<Key> key, throwKey;
    private bool requested, previousRequest, releasePending;
    private float releaseFixedTime, devUntil = -1, primaryUntil = -1, secondaryUntil = -1, throwUntil = -1;
    private bool primaryHeld, secondaryHeld, dropHeld, justEntered;
    private int pendingSlot = -1, rememberedSlot;
    private Character? bound;
    public bool WantsNative => requested || releasePending;
    public string KeyName => key.Value.ToString();
    public string ThrowKeyName => throwKey.Value.ToString();

    public NativeActions(ConfigFile config)
    {
        key = config.Bind("Keys", "PeakEquipment", Key.B, "Hold for original PEAK equipment: 1-4 select, mouse buttons use, hold/release Q throws. Release to return to Minecraft blocks.");
        throwKey = config.Bind("Keys", "PeakThrow", Key.Q, "Hold/release to charge and throw a PEAK item while the equipment key is held.");
    }

    public void UpdateRequest(bool minecraftMode, bool screenOpen, Character c)
    {
        if (!minecraftMode || c == null || LoadingScreenHandler.loading || PeakCraft.World.PeakApi.IsDead(c.data) || c.data.passedOut || c.data.fullyPassedOut) { Reset(); return; }
        if (bound != null && bound != c) Reset();
        bound = c;
        var keyboard = Keyboard.current;
        requested = !screenOpen && ((Application.isFocused && keyboard != null && keyboard[key.Value].isPressed) || Time.unscaledTime < devUntil);
        if (requested && !previousRequest)
        {
            justEntered = true;
            releasePending = false;
            Plugin.Log.LogInfo("equipment: PEAK control requested; hold " + KeyName + ", 1-4 select, mouse uses, Q throws");
        }
        if (!requested && previousRequest)
        {
            // Let PEAK receive the release edges (finish/cancel use and throw) before handoff.
            releasePending = true;
            releaseFixedTime = Time.fixedTime;
        }
        previousRequest = requested;
        if (releasePending && Time.fixedTime > releaseFixedTime)
        {
            releasePending = false;
            Plugin.Log.LogInfo("equipment: returning to Minecraft");
        }
        // CharacterInput already handles the native 1-4 bindings. Polling them here
        // too would issue two equip RPCs for the same key edge.
    }

    public void TickNative(Character c, bool climbing)
    {
        if (c == null || Plugin.Instance.Ownership.State != Owner.NativeControl) return;
        if (climbing)
        {
            if (c.data.currentItem != null) c.refs.items.EquipSlot(Optionable<byte>.None);
            return;
        }
        if (justEntered && requested)
        {
            justEntered = false;
            if (pendingSlot < 0 && c.data.currentItem == null) pendingSlot = rememberedSlot;
        }
        if (pendingSlot >= 0 && requested)
        {
            rememberedSlot = pendingSlot;
            c.refs.items.EquipSlot(Optionable<byte>.Some((byte)pendingSlot));
            Plugin.Log.LogInfo("equipment: selected PEAK slot " + (pendingSlot + 1));
            pendingSlot = -1;
        }
        if (c.data.currentItem != null && c.refs.items.currentSelectedSlot.IsSome)
            rememberedSlot = c.refs.items.currentSelectedSlot.Value;
    }

    public void ApplyInput(CharacterInput input)
    {
        if (!WantsNative) return;
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        bool focus = Application.isFocused;
        bool primary = requested && ((focus && mouse?.leftButton.isPressed == true) || Time.unscaledTime < primaryUntil);
        bool secondary = requested && ((focus && mouse?.rightButton.isPressed == true) || Time.unscaledTime < secondaryUntil);
        bool drop = requested && ((focus && keyboard != null && keyboard[throwKey.Value].isPressed) || Time.unscaledTime < throwUntil);
        input.usePrimaryWasPressed = primary && !primaryHeld;
        input.usePrimaryIsPressed = primary;
        input.usePrimaryWasReleased = !primary && primaryHeld;
        input.useSecondaryWasPressed = secondary && !secondaryHeld;
        input.useSecondaryIsPressed = secondary;
        input.useSecondaryWasReleased = !secondary && secondaryHeld;
        input.dropWasPressed = drop && !dropHeld;
        input.dropIsPressed = drop;
        input.dropWasReleased = !drop && dropHeld;
        primaryHeld = primary; secondaryHeld = secondary; dropHeld = drop;
        var interact = Plugin.Instance.Interact;
        input.interactWasPressed |= interact.Pressed;
        input.interactIsPressed |= interact.Held;
        input.interactWasReleased |= interact.Released;
    }

    public void DevHold(float seconds) => devUntil = Time.unscaledTime + Mathf.Clamp(seconds, 0, 30);
    public void DevSelect(int slot) { pendingSlot = Mathf.Clamp(slot, 0, 3); DevHold(15); }
    public void DevUse(float seconds, bool secondary = false)
    {
        if (secondary) secondaryUntil = Time.unscaledTime + seconds;
        else primaryUntil = Time.unscaledTime + seconds;
        DevHold(seconds + 1);
    }
    public void DevThrow(float seconds) { throwUntil = Time.unscaledTime + seconds; DevHold(seconds + 1); }
    public void Reset()
    {
        requested = previousRequest = releasePending = primaryHeld = secondaryHeld = dropHeld = justEntered = false;
        devUntil = primaryUntil = secondaryUntil = throwUntil = -1;
        pendingSlot = -1; bound = null;
    }

    public void Draw()
    {
        PeakCraft.UI.MinePeakUi.DrawGameplay(Plugin.Instance);
    }
}
