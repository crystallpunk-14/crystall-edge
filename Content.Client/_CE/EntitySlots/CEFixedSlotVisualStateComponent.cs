using System.Numerics;
using Content.Shared._CE.ZLevels.Core.Components;
using Robust.Client.GameObjects;

namespace Content.Client._CE.EntitySlots;

/// <summary>
/// Client-only runtime state for a fixed-slot visual applied to this entity.
/// </summary>
[RegisterComponent]
internal sealed partial class CEFixedSlotVisualStateComponent : Component
{
    public bool Pending;

    public SpriteComponent? OriginalSprite;
    public CEZPhysicsComponent? OriginalZPhysics;
    public Vector2 CleanOffset;
    public Angle? OriginalRotation;
}
