using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Spawns a temporary radiation source at the resolved position.
/// Server-side logic is handled by <c>CEIrradiateEffectSystem</c>.
/// </summary>
public sealed partial class Irradiate : CEEntityEffectBase<Irradiate>
{
    [DataField]
    public float Charge = 10f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-irradiate", ("charge", Charge), ("duration", Duration.TotalSeconds));
}
