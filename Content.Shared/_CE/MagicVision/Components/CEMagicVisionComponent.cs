using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicVision.Components;

/// <summary>
/// Marker applied to an entity that currently perceives with magic vision. Server-authoritative:
/// only ever added/removed by the server's magic vision system (via RefreshMagicVision) - never add
/// or remove this directly, as doing so would desync it from the sources that are supposed to be
/// granting it. The client only ever mirrors this from replicated state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEMagicVisionComponent : Component
{
    /// <summary>
    /// Whether the client should show the screen-distorting overlay on top of just revealing the
    /// hidden magic-vision layer. See <see cref="Events.CECheckMagicVisionEvent.ShowOverlay"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowOverlay;
}
