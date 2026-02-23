using Content.Server._White.StatusEffect.Components;
using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Server._White.StatusEffect;

public sealed partial class WhiteEntityEffectsStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<WhiteEntityEffectsStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var ent, out var entityEffect, out var statusEffect))
        {
            if (entityEffect.NextUpdateTime > _timing.CurTime)
                continue;

            if (statusEffect.AppliedTo is not { } targetUid)
                continue;

            entityEffect.NextUpdateTime = _timing.CurTime + entityEffect.Frequency;
            _entityEffects.ApplyEffects(targetUid, entityEffect.Effects);
        }
    }
}
