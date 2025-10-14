namespace Content.Shared._CE.DayCycle;


[RegisterComponent]
public sealed partial class CEDayCycleComponent : Component
{
    public float LastLightLevel = 0f;

    [DataField]
    public float Threshold = 0.6f;
}
