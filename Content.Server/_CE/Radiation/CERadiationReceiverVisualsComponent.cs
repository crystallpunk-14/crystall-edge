namespace Content.Server._CE.Radiation;

[RegisterComponent, Access(typeof(CERadiationReceiverVisualsSystem))]
public sealed partial class CERadiationReceiverVisualsComponent : Component
{
    [DataField]
    public bool Active;
}
