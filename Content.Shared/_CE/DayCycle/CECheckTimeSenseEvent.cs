using Content.Shared.Inventory;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Raised on an entity to determine whether it can currently sense the time of day.
/// Relayed to worn clothing (except pockets) and to active status effects.
/// </summary>
[ByRefEvent]
public record struct CECheckTimeSenseEvent : IInventoryRelayEvent
{
    public const SlotFlags Slots = SlotFlags.WITHOUT_POCKET;

    public SlotFlags TargetSlots => Slots;

    public bool CanSense;
}

/// <summary>
/// Broadcast after <see cref="CETimeSenseSystem.RefreshTimeSense"/> recalculates whether an entity can sense the time of day
/// </summary>
public readonly record struct CETimeSenseRefreshedEvent(EntityUid Entity, bool CanSense);
