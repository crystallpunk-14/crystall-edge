using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.Placeable;

/// <summary>
/// A rule a tile must satisfy for <see cref="CEPlaceableComponent"/> to place its entity there.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEPlacementCondition
{
    public abstract bool Check(IEntityManager entManager, CEPlacementTarget target);
}

/// <summary>
/// Where a placeable entity is going to appear: a floor tile on some grid, facing a grid-local direction.
/// </summary>
public readonly record struct CEPlacementTarget(EntityUid User, Entity<MapGridComponent> Grid, Vector2i Tile, Direction Direction);
