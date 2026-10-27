using System;
using BepInEx.Configuration;
using PeakCraft.Link;
using PeakCraft.Player;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeakCraft.Hazards;

internal sealed class VitalsSync
{
    private readonly PeakLink link;
    private readonly ConfigEntry<bool> enabled, survivalDefault, showHud;
    private readonly VitalsLedger ledger = new VitalsLedger();
    private Character? character;
    private uint session, sessionCounter = unchecked((uint)Environment.TickCount), guestPid, playerEpoch;
    private bool wasPeakDead, mcDeathNotified, lastMcAlive = true;
    private bool seenLivingGuest, initiallyDead;
    private uint lastGuestSeq, deathCount;
    private float lastLog, startAt;
    private VitalsCommand command;
    private VitalsWire.Guest guest;

    // Engaged also covers the brief initial negotiation, so the old bridge cannot erase statuses.
    public bool Engaged { get; private set; }
    public bool Connected { get; private set; }

    public VitalsSync(PeakLink link, ConfigFile config)
    {
        this.link = link;
        enabled = config.Bind("Vitals", "Enabled", true, "Share Minecraft hearts/food with PEAK Injury/Hunger. Stamina and other afflictions stay native.");
        survivalDefault = config.Bind("Vitals", "StartInSurvival", true, "Start the isolated Minecraft player in Survival when synchronizing. Creative remains available via /gamemode creative.");
        showHud = config.Bind("Vitals", "ShowHud", true, "Show the shared health and food percentages in both play modes.");
    }

    public void Reset()
    {
        character = null;
        session = 0;
        Engaged = Connected = false;
        playerEpoch = 0;
        mcDeathNotified = false;
        wasPeakDead = false;
    }

    private static float Health(Character c) => 20f * (1f - c.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury));
    private static float Food(Character c) => 20f * (1f - c.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger));

    private void Begin(Character c, uint pid, float health, float food)
    {
        character = c;
        guestPid = pid;
        session = ++sessionCounter;
        if (session == 0) session = ++sessionCounter;
        ledger.Reset(health, food);
        ledger.TryGetCommand(out command);
        playerEpoch = 0;
        lastMcAlive = true;
        mcDeathNotified = wasPeakDead = false;
        seenLivingGuest = initiallyDead = false;
        lastGuestSeq = deathCount = 0;
        Connected = false;
        Engaged = true;
        startAt = Time.unscaledTime;
        Plugin.Log.LogInfo($"vitals: new shared player; health {health:F2}/20, food {food:F2}/20");
    }

    public void Update(bool linked, Ownership ownership)
    {
        Character c = Character.localCharacter;
        if (!enabled.Value || !linked || c == null || LoadingScreenHandler.loading)
        {
            Reset();
            Publish(false, false, false);
            return;
        }
        if (character != c || guestPid != link.GuestPid || session == 0)
            Begin(c, link.GuestPid, Health(c), Food(c));

        Connected = link.ReadVitals(out var incoming) && incoming.Magic == VitalsWire.Magic &&
            incoming.Session == session && (incoming.Flags & 1) != 0 &&
            incoming.Heartbeat <= PeakLink.TickCount && PeakLink.TickCount - incoming.Heartbeat < 3000 &&
            Finite(incoming.Health) && Finite(incoming.Food) && incoming.Health >= 0 && incoming.Health <= 20.01f && incoming.Food >= 0 && incoming.Food <= 20.01f;
        if (Connected) guest = incoming;

        bool peakDead = PeakApi.IsDead(c.data);
        if (peakDead)
        {
            // A native lethal event is sent once. An automatic MC respawn must not start a kill loop.
            if (!wasPeakDead)
                command = new VitalsCommand(command.Id + 1, 3, 0, Food(c));
            wasPeakDead = true;
            Publish(true, ownership.BodyFollows, true);
            return;
        }
        if (wasPeakDead)
            Begin(c, link.GuestPid, Health(c), Food(c));

        ledger.SamplePeak(Health(c), Food(c));
        if (Connected && (lastGuestSeq == 0 || unchecked((int)(guest.Seq - lastGuestSeq)) > 0))
        {
            lastGuestSeq = guest.Seq;
            bool alive = (guest.Flags & 2) != 0;
            // A saved Minecraft death screen is not a death in this new PEAK session.
            if (!seenLivingGuest)
            {
                deathCount = guest.DeathCount;
                if (!alive) { initiallyDead = true; Publish(true, ownership.BodyFollows, false); return; }
                if (initiallyDead) { Begin(c, link.GuestPid, Health(c), Food(c)); Publish(true, ownership.BodyFollows, false); return; }
                seenLivingGuest = true;
            }
            bool newDeath = guest.DeathCount != deathCount;
            deathCount = guest.DeathCount;
            // The cumulative counter survives a missed dead snapshot during immediate respawn.
            if ((!alive || newDeath) && !mcDeathNotified)
            {
                mcDeathNotified = true;
                Plugin.Instance.Death.OnMinecraftDeath();
                if (PeakApi.IsDead(c.data)) { Publish(true, ownership.BodyFollows, true); return; }
            }
            // Death wins over queued healing. A fresh living MC player after an airport respawn
            // supplies its fresh vitals; a PEAK revival instead starts from PEAK's revive statuses.
            if (alive && (!lastMcAlive || newDeath) && guest.PlayerEpoch != playerEpoch)
            {
                ApplyPeak(c, guest.Health, guest.Food);
                Begin(c, link.GuestPid, guest.Health, guest.Food);
                Publish(true, ownership.BodyFollows, false);
                return;
            }
            playerEpoch = guest.PlayerEpoch;
            lastMcAlive = alive;
            ledger.ObserveGuest(guest.Health, guest.Food, guest.Ack);
            if (ledger.Initialized)
                ApplyPeak(c, ledger.TargetHealth, ledger.TargetFood);
            if (alive) mcDeathNotified = false;
        }
        if (ledger.TryGetCommand(out var next)) command = next;
        Publish(true, ownership.BodyFollows, false);
        if (Time.unscaledTime - lastLog > 10f)
        {
            lastLog = Time.unscaledTime;
            if (Connected) LogState();
            else if (Time.unscaledTime - startAt > 5f)
                Plugin.Log.LogWarning("vitals: waiting for the local Minecraft vitals addon; PEAK statuses are preserved");
        }
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void ApplyPeak(Character c, float health, float food)
    {
        SetIfChanged(c.refs.afflictions, CharacterAfflictions.STATUSTYPE.Injury, 1f - health / 20f);
        SetIfChanged(c.refs.afflictions, CharacterAfflictions.STATUSTYPE.Hunger, 1f - food / 20f);
        // Use the value PEAK actually accepted (its 2.5% quantization), not the requested value.
        ledger.AcknowledgePeakApplied(Health(c), Food(c));
    }

    private static void SetIfChanged(CharacterAfflictions afflictions, CharacterAfflictions.STATUSTYPE type, float target)
    {
        float quantized = Mathf.Round(Mathf.Clamp01(target) / 0.025f) * 0.025f;
        // Reapplying an unchanged status would erase PEAK's fractional natural-hunger accumulator.
        if (Mathf.Abs(afflictions.GetCurrentStatus(type) - quantized) > 0.0001f)
            afflictions.SetStatus(type, quantized);
    }

    private void Publish(bool active, bool minecraftClock, bool dead)
    {
        var state = new VitalsWire.Host {
            Magic = VitalsWire.Magic, Session = session,
            Flags = (active ? 1u : 0u) | (minecraftClock ? 2u : 0u) | (dead ? 4u : 0u) | (survivalDefault.Value ? 8u : 0u),
            CommandId = command.Id, CommandKind = command.Kind, Health = command.Health, Food = command.Food,
            WorldId = (uint)(SceneManager.GetActiveScene().buildIndex + 1), Heartbeat = PeakLink.TickCount
        };
        link.WriteVitals(in state);
    }

    public void LogState()
    {
        Character c = Character.localCharacter;
        Plugin.Log.LogInfo($"vitals: linked {Connected}, ready {ledger.Initialized}; PEAK health {(c != null ? Health(c) : -1):F2} food {(c != null ? Food(c) : -1):F2}; MC health {guest.Health:F2} food {guest.Food:F2} saturation {guest.Saturation:F2}; ack {guest.Ack}/{command.Id}; epoch {guest.PlayerEpoch}; alive {(guest.Flags & 2) != 0}");
    }

    public void Draw()
    {
        if (!showHud.Value || !Engaged || !Connected || !ledger.Initialized || Ownership.PeakMenuOpen() || Event.current.type != EventType.Repaint) return;
        GUI.Box(new Rect(16, 16, 270, 32), $"Vida: {Mathf.RoundToInt(ledger.TargetHealth * 5)}%     Comida: {Mathf.RoundToInt(ledger.TargetFood * 5)}%");
    }
}
