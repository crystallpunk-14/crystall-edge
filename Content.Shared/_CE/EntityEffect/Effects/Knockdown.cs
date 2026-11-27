using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class Knockdown : CEEntityEffectBase<Knockdown>
{
    [DataField(required: true)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1f);

    [DataField]
    public bool DropItems = false;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        EntityEffectGuidebookText(prototype, entSys, 1f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys, float power) =>
        Loc.GetString("ce-entity-effect-guidebook-knockdown", ("amount", Math.Round((double) ((float) Duration.TotalSeconds * power), 1)));
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