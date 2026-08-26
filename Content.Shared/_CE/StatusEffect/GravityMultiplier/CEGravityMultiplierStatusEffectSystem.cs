using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.StatusEffect.GravityMultiplier;

public sealed partial class CEGravityMultiplierStatusEffectSystem : EntitySystem
{
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGravityMultiplierStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CEGravityMultiplierStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<CEGravityMultiplierStatusEffectComponent, StatusEffectRelayedEvent<CECheckGravityEvent>>(OnCheckGravityState);
    }

    private void OnApplied(Entity<CEGravityMultiplierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
    }

    private void OnRemoved(Entity<CEGravityMultiplierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
    }

    private void OnCheckGravityState(Entity<CEGravityMultiplierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CECheckGravityEvent> args)
    {
        args.Args.Gravity *= ent.Comp.Multiplier;
    }
}
