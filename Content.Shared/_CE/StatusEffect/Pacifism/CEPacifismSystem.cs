using Content.Shared.Interaction.Events;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._CE.StatusEffect.Pacifism;

public sealed partial class CEPacifismSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEPacifismStatusEffectComponent, StatusEffectRelayedEvent<AttackAttemptEvent>>(OnAttackAttempt);
        SubscribeLocalEvent<CEPacifismStatusEffectComponent, StatusEffectRelayedEvent<ShotAttemptedEvent>>(OnShotAttempted);
        SubscribeLocalEvent<CEPacifismStatusEffectComponent, StatusEffectRelayedEvent<BeforeThrowEvent>>(OnBeforeThrow);
    }

    private void OnAttackAttempt(Entity<CEPacifismStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AttackAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnShotAttempted(Entity<CEPacifismStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ShotAttemptedEvent> args)
    {
        var ev = args.Args;
        ev.Cancel();
        args.Args = ev;
    }

    private void OnBeforeThrow(Entity<CEPacifismStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeThrowEvent> args)
    {
        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }
}
