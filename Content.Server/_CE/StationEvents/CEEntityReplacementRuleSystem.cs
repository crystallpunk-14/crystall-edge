using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._CE.StationEvents;

/// <summary>
///
/// </summary>
public sealed class CEEntityReplacementRuleSystem : StationEventSystem<CEEntityReplacementRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected override void Added(EntityUid ruleUld,
        CEEntityReplacementRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleAddedEvent args)
    {
        base.Added(ruleUld, component, gameRule, args);

        List<EntityUid> allEntity = new();

        var replacementCount = component.Range.Next(_random);

        var query = EntityQueryEnumerator<TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var xform, out var meta))
        {
            if (meta.EntityPrototype is null)
                continue;
            if (!component.ReplacementMap.Keys.Contains(meta.EntityPrototype))
                continue;

            allEntity.Add(uid);
        }

        Log.Info($"Replacement count: {replacementCount}");
        Log.Info($"All entity: {allEntity.Count}");
        List<EntityUid> targets = new();
        while (replacementCount > 0 && allEntity.Any())
        {
            var target = allEntity[_random.Next(allEntity.Count)];
            targets.Add(target);
            allEntity.Remove(target);
            replacementCount--;
        }

        Log.Info($"Available targets: {targets.Count}");

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

            Log.Info($"Replace entity from {proto} to {replacement}");
            SpawnAtPosition(replacement, coordinates);
            _audio.PlayPvs(component.ReplaceSound, coordinates);
            QueueDel(target);
        }
    }
}
