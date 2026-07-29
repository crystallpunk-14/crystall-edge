using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.EntityEffect.Systems.StatusEffectApply;

public sealed partial class CEStatusEffectApplyEntityEffectSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEStatusEffectApplyEntityEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out _, out var apply, out var statusEffect))
        {
            if (apply.NextApplyTime > _timing.CurTime)
                continue;

            apply.NextApplyTime = _timing.CurTime + apply.Frequency;

            if (statusEffect.AppliedTo is not { } target)
                continue;

            var args = new CEEntityEffectArgs(EntityManager, target, null, Angle.Zero, 1f, target, Transform(target).Coordinates);
            foreach (var effect in apply.Effects)
            {
                effect.Effect(args);
            }
        }
    }
}
