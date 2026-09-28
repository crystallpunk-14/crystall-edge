namespace Content.Shared._CE.DayCycle;

[RegisterComponent]
public sealed partial class CEDayCycleComponent : Component
{
    public float LastLightLevel = 0f;

    public static float Threshold = 0.6f;
}
