using Content.Shared.Inventory;

namespace Content.Shared._CE.MagicEssence.Events;

/// <summary>
/// Relayed to the eyes slot to check whether the examiner is wearing something (e.g. thaumaturgy
/// glasses) that lets them see essence composition on examine.
/// </summary>
public sealed class CEMagicEssenceScanEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanScan;
    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}
