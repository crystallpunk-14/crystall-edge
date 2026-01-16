using Content.Shared.FixedPoint;
using Content.Shared.Inventory;

namespace Content.Shared._CE.Actions.Events;

/// <summary>
/// An event that checks all sorts of conditions, and calculates the total cost of casting a spell. Called before the spell is cast.
/// </summary>
/// <remarks>TODO: This call is duplicated at the beginning of the cast for checks, and at the end of the cast for mana subtraction.</remarks>
public sealed class CECalculateManacostEvent(EntityUid? performer, FixedPoint2 initialManacost) : EntityEventArgs, IInventoryRelayEvent
{
    public EntityUid? Performer = performer;
    public FixedPoint2 Manacost = initialManacost;

    public float Multiplier = 1f;

    public float TotalManacost => (float)Manacost * Multiplier;

    public SlotFlags TargetSlots { get; } = SlotFlags.All;
}

[ByRefEvent]
public sealed class CESpellFromSpellStorageUsedEvent(
    EntityUid? performer,
    EntityUid? action,
    FixedPoint2 manacost)
    : EntityEventArgs
{
    public EntityUid? Performer { get; init; } = performer;
    public EntityUid? Action { get; init; } = action;
    public FixedPoint2 Manacost { get; init; } = manacost;
}
