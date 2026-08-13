using Content.Shared.EntityConditions;
using Content.Shared.Light.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Checks whether there is a time of day on the current map, and whether the current time of day corresponds to the specified periods.
/// </summary>
public sealed partial class CEIsNightConditionSystem : EntityConditionSystem<TransformComponent, CEIsNightCondition>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CEDayCycleSystem _dayCycle = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<CEIsNightCondition> args)
    {
        var map = _transform.GetMap(entity.Owner);

        if (map is null)
        {
            args.Result = false;
            return;
        }

        args.Result = !_dayCycle.IsDayNow(map.Value);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class CEIsNightCondition : EntityConditionBase<CEIsNightCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}

public sealed partial class CEWeatherAffectConditionSystem : EntityConditionSystem<TransformComponent, CEWeatherAffectCondition>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<CEWeatherAffectCondition> args)
    {
        var gridUid = _transform.GetGrid(entity.Owner);
        if (gridUid is null)
        {
            args.Result = false;
            return;
        }

        if (!TryComp<MapGridComponent>(gridUid.Value, out var grid))
        {
            args.Result = false;
            return;
        }

        TryComp<RoofComponent>(gridUid.Value, out var roof);

        var coordinates = _transform.GetMapCoordinates(entity.Owner);
        var tileRef = _mapSystem.GetTileRef(gridUid.Value, grid, coordinates);

        args.Result = _weather.CanWeatherAffect((gridUid.Value, grid, roof), tileRef);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class CEWeatherAffectCondition : EntityConditionBase<CEWeatherAffectCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}
