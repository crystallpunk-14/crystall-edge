using Content.Shared._CE.Actions.Spells;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.StatusEffect.SpellApply;

public sealed class CEStatusEffectApplySpellSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEStatusEffectApplySpellComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var spell, out var statusEffect))
        {
            if (spell.NextApplyTime > _timing.CurTime)
                continue;

            spell.NextApplyTime = _timing.CurTime + spell.Frequency;

            var spellArgs = new CESpellEffectBaseArgs(statusEffect.AppliedTo, null, statusEffect.AppliedTo, null);
            foreach (var effect in spell.Effects)
            {
                effect.Effect(EntityManager, spellArgs);
            }
        }
    }
}
