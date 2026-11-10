using Content.Shared._CE.EntityEffect.Conditions;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Tries to bring a revivable dead target back to life without touching its damage: works only if the damage
/// is below the death threshold (ending up critical if above the crit one), then pulls the player's ghost back.
/// </summary>
public sealed partial class Revive : CEEntityEffectBase<Revive>;

public sealed partial class CEReviveEffectSystem : CEEntityEffectSystem<Revive>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private CERevivableConditionSystem _revivable = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<Revive> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (_net.IsClient || !_revivable.IsRevivable(entity))
            return;

        if (!_mobThreshold.TryGetThresholdForState(entity, MobState.Dead, out var deadThreshold)
            || _damageable.GetTotalDamage(entity) >= deadThreshold)
            return;

        _mobState.ChangeMobState(entity, MobState.Alive, origin: args.Args.Source);
        _mobThreshold.VerifyThresholds(entity);

        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        if (mind.VisitingEntity is { } visiting
            && _ghostQuery.TryComp(visiting, out var ghost)
            && ghost.CanReturnToBody)
        {
            _mind.UnVisit(mindId, mind);
        }
    }
}
