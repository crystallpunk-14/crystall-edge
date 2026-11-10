using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Changes how strongly the resolved target bleeds. Negative amounts reduce bleeding.
/// </summary>
public sealed partial class ModifyBleeding : CEEntityEffectBase<ModifyBleeding>
{
    [DataField(required: true)]
    public float Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString(Amount < 0
                ? "ce-entity-effect-guidebook-modify-bleeding-reduce"
                : "ce-entity-effect-guidebook-modify-bleeding-increase",
            ("amount", MathF.Abs(Amount)));
    }
}

public sealed partial class CEModifyBleedingEffectSystem : CEEntityEffectSystem<ModifyBleeding>
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(ref CEEntityEffectEvent<ModifyBleeding> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        var wasBleeding = bloodstream.BleedAmount > 0;
        _bloodstream.TryModifyBleedAmount((entity, bloodstream), args.Effect.Amount * args.Args.Power);

        if (wasBleeding && bloodstream.BleedAmount <= 0)
            _popup.PopupClient(Loc.GetString("ce-modify-bleeding-stopped"), entity, args.Args.Source);
    }
}
