using Content.Shared.Stunnable;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class Knockdown : CEEntityEffectBase<Knockdown>
{
    [DataField(required: true)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1f);

    [DataField]
    public bool DropItems = false;
}

public sealed partial class CEKnockdownEffectSystem : CEEntityEffectSystem<Knockdown>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(ref CEEntityEffectEvent<Knockdown> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        var duration = args.Effect.Duration * args.Args.Power;
        _stun.TryKnockdown(entity, duration, drop: args.Effect.DropItems);
    }
}