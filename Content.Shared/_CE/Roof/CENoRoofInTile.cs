using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.Roof;

[UsedImplicitly]
[DataDefinition]
public sealed partial class CENoRoofInTile : IConstructionCondition
{
    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "ce-construction-step-condition-no-roof-in-tile",
        };
    }

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entityManager.System<SharedMapSystem>();
        var transformSystem = entityManager.System<SharedTransformSystem>();

        var grid = transformSystem.GetGrid(user);

        if (grid == null || !entityManager.TryGetComponent<MapGridComponent>(grid, out var gridComp))
        {
            return false;
        }

        var targetPos = transformSystem.ToMapCoordinates(location);
        var anchored = mapSystem.GetAnchoredEntities(grid.Value, gridComp, targetPos);

        foreach (var entt in anchored)
        {
            if (entityManager.HasComponent<CERoofComponent>(entt))
            {
                return false;
            }
        }

        return true;
    }
}
