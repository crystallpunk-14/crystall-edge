using System.Numerics;

namespace Content.Shared._CE.ZLevels.Core.Components;

/// <summary>
/// Lets visual consumers adjust the clean sprite baseline before the canonical
/// client Z-level visual pass commits it for a new component pair.
/// </summary>
[ByRefEvent]
public record struct CEZVisualBaselineQueryEvent(
    CEZPhysicsComponent Component,
    Vector2 SpriteOffsetBaseline);

/// <summary>
/// Raised by the canonical Z-physics lifecycle owner before its visual baseline is removed.
/// </summary>
[ByRefEvent]
public readonly record struct CEZVisualBaselineReleasingEvent(CEZPhysicsComponent Component);
