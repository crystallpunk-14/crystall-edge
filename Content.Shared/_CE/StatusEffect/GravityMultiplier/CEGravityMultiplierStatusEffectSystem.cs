using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.StatusEffect.GravityMultiplier;

public sealed partial class CEGravityMultiplierStatusEffectSystem : EntitySystem
{
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CEGravityMultiplierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEGravityMultiplierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnCheckGravityState(Entity<CEGravityMultiplierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CECheckGravityEvent> args)
    {
        args.Args.Gravity *= ent.Comp.Multiplier;
    }
}
