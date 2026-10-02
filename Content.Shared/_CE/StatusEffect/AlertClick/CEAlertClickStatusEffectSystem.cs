using Content.Shared._CE.EntityEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._CE.StatusEffect.AlertClick;

public sealed partial class CEAlertClickStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private EntityQuery<StatusEffectAlertComponent> _alertQuery = default!;

    [SubscribeLocalEvent]
    private void OnAlertClick(Entity<StatusEffectContainerComponent> ent, ref CEStatusEffectAlertClickEvent args)
    {
        if (args.Handled)
            return;

        // Effects may add or remove status effects, so collect the matches before running anything.
        List<CEAlertClickStatusEffectComponent>? matches = null;
        foreach (var effect in _statusEffects.EnumerateStatusEffects<CEAlertClickStatusEffectComponent>(ent.AsNullable()))
        {
            if (!_alertQuery.TryComp(effect, out var alert) || alert.Alert != args.AlertId)
                continue;

            matches ??= new();
            matches.Add(effect.Comp2);
        }

        if (matches == null)
            return;

        var effectArgs = new CEEntityEffectArgs(EntityManager, ent, null, Angle.Zero, 0f, ent, null);
        foreach (var match in matches)
        {
            foreach (var effect in match.Effects)
            {
                effect.Effect(effectArgs);
            }
        }

        args.Handled = true;
    }
}
