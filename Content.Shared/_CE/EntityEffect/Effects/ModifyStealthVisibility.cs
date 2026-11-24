using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Shifts the effect entity's <see cref="StealthComponent"/> visibility.
/// Positive <see cref="Amount"/> reveals a cloaked entity, negative conceals it further.
/// No-op if the entity has no <see cref="StealthComponent"/>.
/// </summary>
public sealed partial class ModifyStealthVisibility : CEEntityEffectBase<ModifyStealthVisibility>
{
    /// <summary>
    /// Flat visibility delta to apply. The visual scale runs from -1 (fully hidden) to 1 (fully visible),
    /// so a positive value partially reveals a cloaked entity.
    /// </summary>
    [DataField]
    public float Amount = 0.5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        EntityEffectGuidebookText(prototype, entSys, 1f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys, float power) =>
        Loc.GetString("ce-entity-effect-guidebook-modify-stealth-visibility", ("amount", Math.Round((double) (Amount * power), 1)));
}

public sealed partial class CEModifyStealthVisibilitySystem : CEEntityEffectSystem<ModifyStealthVisibility>
{
    [Dependency] private SharedStealthSystem _stealth = default!;

    protected override void Effect(ref CEEntityEffectEvent<ModifyStealthVisibility> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!HasComp<StealthComponent>(entity))
            return;

        _stealth.ModifyVisibility(entity, args.Effect.Amount * args.Args.Power);
    }
}
