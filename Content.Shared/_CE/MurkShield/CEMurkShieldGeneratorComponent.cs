using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MurkShield;

/// <summary>
/// Creates the murk shield at the moment of dawn, if powered. With several generators only one random
/// powered generator creates it, so there is never more than one shield.
/// </summary>
[RegisterComponent]
public sealed partial class CEMurkShieldGeneratorComponent : Component
{
    [DataField]
    public EntProtoId Shield = "CEMurkShield";
}
