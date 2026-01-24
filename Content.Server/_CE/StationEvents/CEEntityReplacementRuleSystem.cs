using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Station event system that randomly replaces selected entities with mapped prototypes,
/// optionally playing visual and audio effects at the replacement location.
public sealed class CEEntityReplacementRuleSystem : StationEventSystem<CEEntityReplacementRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
}
