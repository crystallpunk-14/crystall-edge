using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Makes the resolved entity vomit, adjusting its hunger and thirst.
/// Server-side logic is handled by <c>CEVomitEffectSystem</c>.
/// </summary>
public sealed partial class Vomit : CEEntityEffectBase<Vomit>
{
    [DataField]
    public float Probability = 1f;

    [DataField]
    public float ThirstAmount = -8f;

    [DataField]
    public float HungerAmount = -8f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-vomit", ("chance", Probability * 100));
}
