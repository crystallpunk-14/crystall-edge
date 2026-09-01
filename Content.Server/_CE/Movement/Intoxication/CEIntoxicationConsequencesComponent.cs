using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Movement.Intoxication;

/// <summary>
/// Opts an entity into additional consequences driven by vanilla drunkenness.
/// </summary>
[RegisterComponent]
public sealed partial class CEIntoxicationConsequencesComponent : Component
{
    [DataField]
    public EntProtoId DrowsinessStatusEffect = "CEStatusEffectIntoxicationDrowsiness";

    [DataField(required: true)]
    public LocId ExamineMessage = string.Empty;
}
