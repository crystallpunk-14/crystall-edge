using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.StatusEffect.ChasmProtection;

public sealed partial class CEChasmProtectionStatusEffectSystem : EntitySystem
{
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysicsQuery = default!;

    [SubscribeLocalEvent]
    private void OnChasmAttempt(Entity<CEChasmProtectionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CEZLevelChasmAttempt> args)
    {
        if (args.Args.Cancelled)
            return;

        args.Args.Cancel();

        var target = args.Args.Falled;
        if (!_zPhysicsQuery.TryComp(target, out var zPhysics))
            return;

        _zLevels.SetZPosition((target, zPhysics), 0f);

        if (zPhysics.Velocity < 0)
            _zLevels.SetZVelocity((target, zPhysics), 0f);
    }
}
