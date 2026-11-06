using System.Numerics;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Footprints;

/// <summary>
/// A single footprint or drag smear left on a tile. Many of them may share one tile.
/// The reagents of the print live in the entity's own solution.
/// The entity is anchored to the tile center, so the visual position inside the tile is stored in <see cref="Offset"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CEFootprintComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Sprite;

    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    /// <summary>
    /// Sprite offset from the tile center, in grid-local coordinates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Offset;

    /// <summary>
    /// Sprite rotation relative to the grid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Rotation;

    [DataField]
    public string SolutionName = "footprint";

    /// <summary>
    /// Total footprint volume on a tile at which all footprints there turn into a puddle.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxTileVolume = 20;
}
