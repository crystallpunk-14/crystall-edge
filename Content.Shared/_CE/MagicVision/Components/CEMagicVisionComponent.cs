using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicVision.Components;

/// <summary>
/// Marker applied to an entity that currently perceives with magic vision. Server-authoritative:
/// only ever added/removed by the server's magic vision system (via RefreshMagicVision) - never add
/// or remove this directly, as doing so would desync it from the sources that are supposed to be
/// granting it. The client only ever mirrors this from replicated state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMagicVisionComponent : Component;
