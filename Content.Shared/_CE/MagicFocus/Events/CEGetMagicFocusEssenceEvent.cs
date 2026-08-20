using Content.Shared.Inventory;

namespace Content.Shared._CE.MagicFocus.Events;

/// <summary>
/// Raised on a user (relayed to equipped clothing) and manually on held items to collect every
/// entity currently acting as a magic focus for them. Handlers just register themselves in
/// <see cref="Sources"/> — <see cref="Content.Shared._CE.MagicFocus.Systems.CEMagicFocusSystem"/>
/// does the actual essence accounting.
/// </summary>
public sealed class CEGetMagicFocusEssenceEvent : EntityEventArgs, IInventoryRelayEvent
{
    public readonly List<EntityUid> Sources = new();

    public SlotFlags TargetSlots => SlotFlags.All;
}
