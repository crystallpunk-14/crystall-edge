using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._CE.StatusEffect.MysticVitality;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Server._CE.StatusEffect.MysticVitality;

/// <summary>
/// Refills the breath of critical <see cref="CEMysticVitalityStatusEffectComponent"/> holders whenever they take damage.
/// </summary>
public sealed partial class CEMysticVitalityBreathingSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [SubscribeLocalEvent]
    private void OnDamageChanged(EntityUid uid, RespiratorComponent respirator, DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            !_mobState.IsCritical(uid) ||
            !_statusEffects.TryEffectsWithComp<CEMysticVitalityStatusEffectComponent>(uid, out var effects))
        {
            return;
        }

        foreach (var effect in effects)
        {
            if (!effect.Comp1.PreventCritSuffocation)
                continue;

            // Saturation is clamped to its maximum, so this refills it completely.
            _respirator.UpdateSaturation(uid, float.MaxValue, respirator);
            return;
        }
    }
}
