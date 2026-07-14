namespace Content.Shared._CE.EntityEffect.Effects;


public sealed partial class Delete : CEEntityEffectBase<Delete>
{
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
