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
/// Raised when an entity enters the stamina critical state, allowing other systems (such as the flight system)
/// to react appropriately (for example, disabling or modifying flight behavior).
/// </summary>
/// <remarks>
/// CrystallEdge: Need stamina event for stamina flyer
/// </remarks>
public sealed class CEEnterStaminaCritEvent : EntityEventArgs;
