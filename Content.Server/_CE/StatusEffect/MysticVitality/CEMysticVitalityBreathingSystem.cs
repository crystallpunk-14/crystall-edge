using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._CE.StatusEffect.MysticVitality;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Server._CE.StatusEffect.MysticVitality;

/// <summary>
/// Keeps critical targets of <see cref="CEMysticVitalityStatusEffectComponent"/> fully saturated, so they never suffocate.
/// </summary>
public sealed partial class CEMysticVitalityBreathingSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RespiratorSystem _respirator = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMysticVitalityStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var vitality, out var status))
        {
            if (!vitality.PreventCritSuffocation ||
                status.AppliedTo is not { } target ||
                !_mobState.IsCritical(target) ||
                !TryComp<RespiratorComponent>(target, out var respirator))
            {
                continue;
            }

            // Saturation is clamped to its maximum, so this refills it completely.
            _respirator.UpdateSaturation(target, float.MaxValue, respirator);
        }
    }
}
