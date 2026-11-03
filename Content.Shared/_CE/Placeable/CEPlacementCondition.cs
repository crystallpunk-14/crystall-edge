using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable;

/// <summary>
/// A rule a tile must satisfy for <see cref="CEPlaceableComponent"/> to place its entity there.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEPlacementCondition
{
    /// <summary>
    /// Shown to the user when this condition fails. Falls back to <see cref="DefaultPopup"/>.
    /// </summary>
    [DataField]
    public LocId? Popup;

    protected virtual LocId DefaultPopup => "ce-placeable-cannot-place";

    public abstract bool Check(IEntityManager entManager, CEPlacementTarget target);

    public virtual string GetPopup(IEntityManager entManager, CEPlacementTarget target)
    {
        return Loc.GetString(Popup ?? DefaultPopup);
    }
}

/// <summary>
/// Where a placeable entity is going to appear: a floor tile on some grid, facing a grid-local direction.
/// </summary>
public readonly record struct CEPlacementTarget(
    EntityUid User,
    EntProtoId Prototype,
    Entity<MapGridComponent> Grid,
    Vector2i Tile,
    Direction Direction);
