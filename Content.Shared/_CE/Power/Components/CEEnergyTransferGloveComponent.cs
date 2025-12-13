namespace Content.Shared._CE.Power.Components;

/// <summary>
/// TODO
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class CEEnergyTransferGloveComponent : Component
{
    [DataField]
    public float TransferAmount = 5f;

    /// <summary>
    /// true = drain from target, false = transfer to target
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ConsumeMode = true;

    [DataField]
    public float ThrowPower = 5f;

    [DataField]
    public float ThrowDistance = 1f;

    [DataField]
    public float PullDistance = 1f;
}
