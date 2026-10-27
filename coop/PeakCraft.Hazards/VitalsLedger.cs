using System;

namespace PeakCraft.Hazards;

/// <summary>
/// Reconciles the local PEAK status bars with Minecraft's authoritative vitals.
/// Only one PEAK command is in flight: later local changes remain queued until
/// Minecraft acknowledges it. Normal commands contain deltas, so damage or food
/// consumed by Minecraft while a command travels is not overwritten.
/// </summary>
internal sealed class VitalsLedger
{
    private const float Epsilon = 0.0001f;

    private float peakHealth;
    private float peakFood;
    private float pendingHealth;
    private float pendingFood;
    private float guestHealth;
    private float guestFood;
    private uint issuedId;
    private uint acknowledgedId;
    private bool started;
    private bool inFlight;
    private VitalsCommand command;

    public float TargetHealth { get; private set; }
    public float TargetFood { get; private set; }
    public bool Initialized { get; private set; }

    /// <summary>A new wire session must accompany Reset; command IDs restart at one.</summary>
    public void Reset(float health, float food)
    {
        peakHealth = Clamp(Finite(health) ? health : 20f);
        peakFood = Clamp(Finite(food) ? food : 20f);
        guestHealth = peakHealth;
        guestFood = peakFood;
        pendingHealth = pendingFood = 0f;
        TargetHealth = peakHealth;
        TargetFood = peakFood;
        issuedId = 1;
        acknowledgedId = 0;
        command = new VitalsCommand(issuedId, VitalsCommand.Absolute, peakHealth, peakFood);
        started = inFlight = true;
        Initialized = false;
    }

    /// <summary>Call before reflecting a remote value into the PEAK status bars.</summary>
    public void SamplePeak(float health, float food)
    {
        if (!started) return;
        if (Finite(health))
        {
            health = Clamp(health);
            pendingHealth += health - peakHealth;
            peakHealth = health;
        }
        if (Finite(food))
        {
            food = Clamp(food);
            pendingFood += food - peakFood;
            peakFood = food;
        }
        RecomputeTarget();
    }

    /// <summary>
    /// PEAK rounds statuses to 0.025. Record the value actually applied, rather
    /// than the requested value, so that rounding and remote updates never echo
    /// back as a new local heal, hit or meal. Queued local changes are preserved.
    /// </summary>
    public void AcknowledgePeakApplied(float actualHealth, float actualFood)
    {
        if (!started) return;
        if (Finite(actualHealth)) peakHealth = Clamp(actualHealth);
        if (Finite(actualFood)) peakFood = Clamp(actualFood);
    }

    /// <summary>
    /// The caller validates the wire session and snapshot sequence first. An
    /// older acknowledgement cannot roll back state after a later one arrived.
    /// </summary>
    public void ObserveGuest(float health, float food, uint ack)
    {
        if (!started || !Finite(health) || !Finite(food)) return;
        if (ack < acknowledgedId || ack > issuedId) return;
        if (!Initialized && ack != command.Id) return;

        if (inFlight && ack == command.Id)
        {
            inFlight = false;
            Initialized = true;
        }
        acknowledgedId = ack;
        guestHealth = Clamp(health);
        guestFood = Clamp(food);
        RecomputeTarget();
    }

    /// <summary>
    /// Retransmits the same outstanding command until its acknowledgement.
    /// Pending changes are sent once, after the previous command was consumed.
    /// </summary>
    public bool TryGetCommand(out VitalsCommand result)
    {
        if (!started)
        {
            result = default;
            return false;
        }
        if (inFlight)
        {
            result = command;
            return true;
        }
        if (!Initialized || (Math.Abs(pendingHealth) < Epsilon && Math.Abs(pendingFood) < Epsilon))
        {
            result = default;
            return false;
        }

        issuedId++;
        // A session has a finite lifetime; reserve zero even at integer wrap.
        if (issuedId == 0) issuedId = 1;
        command = new VitalsCommand(issuedId, VitalsCommand.Delta, pendingHealth, pendingFood);
        pendingHealth = pendingFood = 0f;
        inFlight = true;
        RecomputeTarget();
        result = command;
        return true;
    }

    private void RecomputeTarget()
    {
        float health = guestHealth + pendingHealth;
        float food = guestFood + pendingFood;
        if (inFlight && command.Kind == VitalsCommand.Delta)
        {
            health += command.Health;
            food += command.Food;
        }
        TargetHealth = Clamp(health);
        TargetFood = Clamp(food);
    }

    private static float Clamp(float value) => Math.Max(0f, Math.Min(20f, value));
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

internal readonly struct VitalsCommand
{
    public const uint Absolute = 1;
    public const uint Delta = 2;

    public readonly uint Id;
    public readonly uint Kind;
    public readonly float Health;
    public readonly float Food;

    public VitalsCommand(uint id, uint kind, float health, float food)
    {
        Id = id;
        Kind = kind;
        Health = health;
        Food = food;
    }
}
