using Content.Shared.Inventory;

namespace Content.Shared._CE.MagicVision.Events;

/// <summary>
/// Raised on an entity to determine whether it should currently have magic vision. Any source that
/// wants to grant magic vision (clothing, skills, etc.) should hook into this event and call
/// <see cref="GrantVision"/>. Relayed to the eyes slot so worn clothing can respond too.
/// Server-side only - raised by the server's magic vision system's RefreshMagicVision method.
/// </summary>
public sealed class CECheckMagicVisionEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;

    public bool HasVision { get; private set; }

    public void GrantVision()
    {
        HasVision = true;
    }
}
