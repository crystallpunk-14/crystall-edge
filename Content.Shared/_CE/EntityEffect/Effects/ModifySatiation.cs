using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Changes the target's satiation of the given type (hunger, thirst). A negative amount makes the target hungrier.
/// </summary>
public sealed partial class ModifySatiation : CEEntityEffectBase<ModifySatiation>
{
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> SatiationType;

    [DataField]
    public float Amount = -5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        EntityEffectGuidebookText(prototype, entSys, 1f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys, float power) =>
        Loc.GetString("ce-entity-effect-guidebook-modify-satiation",
            ("amount", Math.Round((double) (Amount * power), 1)),
            ("type", prototype.Index(SatiationType).Name));
}

public sealed partial class CEModifySatiationEffectSystem : CEEntityEffectSystem<ModifySatiation>
{
    [Dependency] private SatiationSystem _satiation = default!;

    protected override void Effect(ref CEEntityEffectEvent<ModifySatiation> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryComp<SatiationComponent>(entity, out var satiation))
            return;

        _satiation.ModifyValue((entity, satiation), args.Effect.SatiationType, args.Effect.Amount * args.Args.Power);
    }
}
