namespace Content.Client._CE.Wave;

[RegisterComponent]
[Access(typeof(CEWaveShaderSystem))]
public sealed partial class CEWaveShaderComponent : Component
{
    [DataField]
    public float Speed = 10f;

    [DataField]
    public float Dis = 10f;

    [DataField]
    public float Offset = 0f;
}
