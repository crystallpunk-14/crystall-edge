using Content.Shared._CE.Fluids;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.Fluids;

/// <summary>
/// Evaporates every reagent of <see cref="CEOutdoorEvaporationComponent"/> entities standing under the open sky.
/// </summary>
public sealed partial class CEOutdoorEvaporationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEOutdoorEvaporationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CEOutdoorEvaporationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var evaporation, out var xform))
        {
            if (evaporation.NextUpdate > curTime)
                continue;

            evaporation.NextUpdate = curTime + evaporation.Interval;

            if (!IsUnderOpenSky(xform))
                continue;

            if (!_solution.TryGetSolution(uid, evaporation.Solution, out var solutionEnt, out var solution))
                continue;

            if (solution.Volume > FixedPoint2.Zero)
            {
                solution.SplitSolution(evaporation.Speed * evaporation.Interval.TotalSeconds);
                _solution.UpdateChemicals(solutionEnt.Value, false);
            }

            if (evaporation.DeleteOnEmpty && solution.Volume <= FixedPoint2.Zero)
                QueueDel(uid);
        }
    }

    public bool IsUnderOpenSky(TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return false;

        var tileRef = _map.GetTileRef((gridUid, grid), xform.Coordinates);
        return _weather.CanWeatherAffect((gridUid, grid, null), tileRef);
    }
}
