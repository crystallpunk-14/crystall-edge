using Content.Shared._CE.MagicVision;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicVision.Components;

/// <summary>
/// Marker applied to an entity that currently perceives with magic vision. Only ever added/removed
/// by <see cref="CESharedMagicVisionSystem.RefreshMagicVision"/> - never add or remove this directly,
/// as doing so would desync it from the sources that are supposed to be granting it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMagicVisionComponent : Component;
