namespace Content.Server._CE.Power.Components;

[RegisterComponent]
public sealed partial class CEPipeVisComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public string Node = "";
}
