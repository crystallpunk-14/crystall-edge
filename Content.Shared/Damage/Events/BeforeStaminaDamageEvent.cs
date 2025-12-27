using Content.Shared.Inventory;

namespace Content.Shared.Damage.Events;

/// <summary>
/// Raised before stamina damage is dealt to allow other systems to cancel or modify it.
/// </summary>
[ByRefEvent]
public record struct BeforeStaminaDamageEvent(float Value, bool Cancelled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots =>  ~SlotFlags.POCKET;
}

/// <summary>
/// CrystallEdge: Need stamina event for stamina flyer
/// </summary>
public sealed class CEEnterStaminaCritEvent : EntityEventArgs;
