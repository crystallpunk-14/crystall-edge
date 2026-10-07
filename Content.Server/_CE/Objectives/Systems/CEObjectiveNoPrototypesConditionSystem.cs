using Content.Server._CE.Objectives.Components;
using Content.Server.Station.Systems;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveNoPrototypesConditionComponent"/>.
/// </summary>
public sealed partial class CEObjectiveNoPrototypesConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private StationSystem _station = default!;

    [SubscribeLocalEvent]
    private void OnInitialize(Entity<CEObjectiveNoPrototypesConditionComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        ent.Comp.InitialCount = CountRemaining(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveNoPrototypesConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var remaining = CountRemaining(ent.Comp);

        if (ent.Comp.InitialCount <= 0)
        {
            args.Progress = remaining == 0 ? 1f : 0f;
            return;
        }

        args.Progress = Math.Clamp(1f - (float) remaining / ent.Comp.InitialCount, 0f, 1f);
    }

    // Broadcast by the engine for every deleted entity - only matching prototypes trigger a recount.
    [SubscribeLocalEvent]
    private void OnEntityTerminating(ref EntityTerminatingEvent args)
    {
        if (args.Entity.Comp.EntityPrototype is not { } proto)
            return;

        var query = EntityQueryEnumerator<CEObjectiveNoPrototypesConditionComponent>();
        while (query.MoveNext(out var uid, out var condition))
        {
            if (condition.Prototypes.Contains(proto.ID))
                _objectives.RefreshObjectiveProgress(uid);
        }
    }

    private int CountRemaining(CEObjectiveNoPrototypesConditionComponent condition)
    {
        var maps = GetStationMaps();
        var count = 0;

        var query = EntityQueryEnumerator<MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var meta, out var xform))
        {
            if (meta.EntityPrototype is not { } proto ||
                !condition.Prototypes.Contains(proto.ID) ||
                meta.EntityLifeStage >= EntityLifeStage.Terminating ||
                xform.MapUid is not { } map ||
                !maps.Contains(map))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    /// <summary>
    /// Every map of the z-network the station sits on - or just its own map, if it has no network.
    /// </summary>
    private HashSet<EntityUid> GetStationMaps()
    {
        var maps = new HashSet<EntityUid>();
        foreach (var station in _station.GetStations())
        {
            if (_station.GetLargestGrid(station) is not { } grid ||
                Transform(grid).MapUid is not { } stationMap)
            {
                continue;
            }

            maps.Add(stationMap);

            if (!_zLevels.TryGetMapNetwork(stationMap, out var network))
                continue;

            foreach (var map in network.Comp.ZLevels.Values)
            {
                if (map is { } level)
                    maps.Add(level);
            }
        }

        return maps;
    }
}
