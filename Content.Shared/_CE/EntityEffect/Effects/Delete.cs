using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;


public sealed partial class Delete : CEEntityEffectBase<Delete>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-delete");
}

public sealed partial class CEQueueDelEffectSystem : CEEntityEffectSystem<Delete>
{
    protected override void Effect(ref CEEntityEffectEvent<Delete> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        PredictedQueueDel(entity);
    }
}
