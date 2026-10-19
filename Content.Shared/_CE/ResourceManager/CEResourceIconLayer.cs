using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.ResourceManager;

/// <summary>
/// One layer of a resource requirement icon: either a flat sprite or a whole entity prototype sprite.
/// Offset and scale are fractions of the icon size; positive Y goes down.
/// </summary>
public sealed record CEResourceIconLayer
{
    public SpriteSpecifier? Sprite { get; init; }

    public EntProtoId? Entity { get; init; }

    public Vector2 Offset { get; init; }

    public float Scale { get; init; } = 1f;

    public Color Color { get; init; } = Color.White;

    public static CEResourceIconLayer FromSprite(SpriteSpecifier sprite, Color? color = null)
    {
        return new CEResourceIconLayer { Sprite = sprite, Color = color ?? Color.White };
    }

    public static CEResourceIconLayer FromEntity(EntProtoId entity, Color? color = null)
    {
        return new CEResourceIconLayer { Entity = entity, Color = color ?? Color.White };
    }
}
