using Content.Shared.Interaction.Events;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.StatusEffect.Pacifism;

public sealed partial class CEPacifismSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void OnAttackAttempt(Entity<CEPacifismStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AttackAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<CEPacifismStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ShotAttemptedEvent> args)
    {
        var ev = args.Args;
        ev.Cancel();
        args.Args = ev;
    }

    [SubscribeLocalEvent]
    private void OnBeforeThrow(Entity<CEPacifismStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeThrowEvent> args)
    {
        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }
}
