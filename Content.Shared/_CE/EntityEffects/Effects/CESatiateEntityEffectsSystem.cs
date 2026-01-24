using Content.Shared._CE.Satiation;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffects.Effects;
public sealed partial class CESatiateEntityEffectsSystem : EntityEffectSystem<CESatiationsComponent, CESatiate>
{
    [Dependency] private readonly CESharedSatiationSystem _satiation = default!;

    protected override void Effect(Entity<CESatiationsComponent> entity, ref EntityEffectEvent<CESatiate> args)
    {
        _satiation.EditSatiationLevel((entity, entity.Comp), args.Effect.SatiationType, args.Effect.Amount * args.Scale);
    }
}

public sealed partial class CESatiate : EntityEffectBase<CESatiate>
{
    [DataField(required: true)]
    public ProtoId<CESatiationTypePrototype> SatiationType;

    [DataField(required: true)]
    public float Amount;
}
