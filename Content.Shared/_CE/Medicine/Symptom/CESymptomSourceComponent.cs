using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Medicine.Symptom;

/// <summary>
/// Status effect that grants symptom status effects to its bearer while it is applied.
/// Symptoms shared by several sources exist once and live while at least one source is applied.
/// </summary>
[RegisterComponent]
public sealed partial class CESymptomSourceComponent : Component
{
    /// <summary>
    /// Symptom status effects granted to the bearer. A symptom must not carry this component itself.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Symptoms = new();
}
