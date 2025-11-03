namespace Content.Client._CE.Power.Components;

[RegisterComponent]
public sealed partial class CEPipeVisualizerComponent : Component
{
    [DataField]
    public string? StatePrefix;

    [DataField]
    public string? ExtraLayerPrefix;
}
