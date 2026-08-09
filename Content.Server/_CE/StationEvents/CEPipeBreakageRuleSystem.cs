using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Station event system that picks a random center pipe, always breaks it, then breaks every nearby pipe
/// within a radius with an independent chance.
/// </summary>
public sealed partial class CEPipeBreakageRuleSystem : StationEventSystem<CEPipeBreakageRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly List<EntityUid> _anchoredEntities = new();

    protected override void Started(EntityUid ruleUid,
        CEPipeBreakageRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(ruleUid, component, gameRule, args);

        var centerCandidates = new List<EntityUid>();

        var query = EntityQueryEnumerator<TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var xform, out var meta))
        {
            if (meta.EntityPrototype is null || !component.CenterPrototype.Equals(meta.EntityPrototype))
                continue;
            if (HasOtherAnchoredEntities(uid, xform))
                continue;

            centerCandidates.Add(uid);
        }

        if (centerCandidates.Count == 0)
            return;

        var center = _random.Pick(centerCandidates);
        var centerXform = Transform(center);
        var centerCoords = centerXform.Coordinates;
        var mapCoords = _transform.GetMapCoordinates(center, centerXform);

        BreakPipe(component, center, centerCoords, alwaysBreak: true, spawnVfx: true);

        foreach (var (uid, meta) in _lookup.GetEntitiesInRange<MetaDataComponent>(mapCoords, component.Radius))
        {
            if (uid == center)
                continue;
            if (meta.EntityPrototype is null || !component.ReplacementMap.ContainsKey(meta.EntityPrototype))
                continue;
            if (!TryComp<TransformComponent>(uid, out var xform) || HasOtherAnchoredEntities(uid, xform))
                continue;

            BreakPipe(component, uid, Transform(uid).Coordinates, alwaysBreak: false, spawnVfx: false);
        }
    }

    private void BreakPipe(CEPipeBreakageRuleComponent component,
        EntityUid target,
        EntityCoordinates coordinates,
        bool alwaysBreak,
        bool spawnVfx)
    {
        if (!alwaysBreak && !_random.Prob(component.BreakChance))
            return;

        var proto = MetaData(target).EntityPrototype;
        if (proto is null || !component.ReplacementMap.TryGetValue(proto, out var replacement))
            return;

        if (spawnVfx && component.CenterVfx is not null)
            SpawnAtPosition(component.CenterVfx, coordinates);

        SpawnAtPosition(replacement, coordinates);
        _audio.PlayPvs(component.BreakSound, coordinates);
        QueueDel(target);
    }

    /// <summary>
    /// Checks whether any anchored entity other than <paramref name="uid"/> itself occupies the same tile.
    /// Used to avoid replacing pipes sharing a tile with walls, carpets, etc.
    /// </summary>
    private bool HasOtherAnchoredEntities(EntityUid uid, TransformComponent xform)
    {
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var tile = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((grid, gridComp), tile, _anchoredEntities);

        foreach (var other in _anchoredEntities)
        {
            if (other != uid)
                return true;
        }

        return false;
    }
}
