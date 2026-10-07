using Content.Shared.Inventory;

namespace Content.Shared._CE.MagicVision.Events;

/// <summary>
/// Raised on an entity to determine whether it should currently have magic vision. Any source that
/// wants to grant magic vision (clothing, skills, etc.) should hook into this event and call
/// <see cref="GrantVision"/>. Relayed to all worn slots (except pockets) so worn clothing can respond too.
/// Server-side only - raised by the server's magic vision system's RefreshMagicVision method.
/// </summary>
public sealed class CECheckMagicVisionEvent : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    /// Slots whose worn clothing can grant magic vision.
    /// </summary>
    public const SlotFlags Slots = SlotFlags.WITHOUT_POCKET;

    public SlotFlags TargetSlots { get; } = Slots;

    public bool HasVision { get; private set; }

    /// <summary>
    /// Whether the client's screen-distorting overlay should be shown. Innate sources (e.g. ghosts,
    /// who always perceive magic) opt out so the effect doesn't nag them constantly - it's meant to
    /// represent the temporary strain of a worn artifact, not a permanent ability.
    /// </summary>
    public bool ShowOverlay { get; private set; }

    public void GrantVision(bool showOverlay = true)
    {
        HasVision = true;
        ShowOverlay |= showOverlay;
    }
}
