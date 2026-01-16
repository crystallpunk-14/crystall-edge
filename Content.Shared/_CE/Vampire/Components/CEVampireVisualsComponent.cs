using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Vampire;

[RegisterComponent, NetworkedComponent]
public sealed partial class CEVampireVisualsComponent : Component
{
    [DataField]
    public Color EyesColor = Color.Red;

    [DataField]
    public Color OriginalEyesColor = Color.White;

    [DataField]
    public string FangsMap = "vampire_fangs";

    [DataField]
    public EntProtoId EnableVFX = "CEImpactEffectBloodEssence2";

    [DataField]
    public EntProtoId DisableVFX = "CEImpactEffectBloodEssenceInverse";
}
