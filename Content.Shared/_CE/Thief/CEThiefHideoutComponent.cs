namespace Content.Shared._CE.Thief;

/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class CEThiefHideoutComponent : Component
{
    [DataField]
    public float ScanRange = 2f;

    [DataField]
    public EntityUid? ThiefMind;

    [DataField]
    public float MaxScore;
}
