using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Station event system that randomly replaces selected entities with mapped prototypes,
/// optionally playing visual and audio effects at the replacement location.
public sealed partial class CEEntityReplacementRuleSystem : StationEventSystem<CEEntityReplacementRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MapSystem _map = default!;

    private readonly List<EntityUid> _anchoredEntities = new();

    protected override void Started(EntityUid ruleUid,
        CEEntityReplacementRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(ruleUid, component, gameRule, args);

        List<EntityUid> allEntities = new();

        var replacementCount = component.Range.Next(_random);

        var query = EntityQueryEnumerator<TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var xform, out var meta))
        {
            if (meta.EntityPrototype is null)
                continue;
            if (!component.ReplacementMap.Keys.Contains(meta.EntityPrototype))
                continue;
            if (HasOtherAnchoredEntities(uid, xform))
                continue;

            allEntities.Add(uid);
        }

        var targetCount = Math.Min(replacementCount, allEntities.Count);
        _random.Shuffle(allEntities);
        var targets = new List<EntityUid>(targetCount);
        for (var i = 0; i < targetCount; i++)
        {
            targets.Add(allEntities[i]);
        }

        foreach (var target in targets)
        {
            var coordinates = Transform(target).Coordinates;
            var proto = MetaData(target).EntityPrototype;

            if (proto is null)
                continue;

            if (component.ReplaceVfx is not null)
                SpawnAtPosition(component.ReplaceVfx, coordinates);

            if (!component.ReplacementMap.TryGetValue(proto, out var replacement))
                continue;

            SpawnAtPosition(replacement, coordinates);
            _audio.PlayPvs(component.ReplaceSound, coordinates);
            QueueDel(target);
        }
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
