namespace Content.Shared._CE.Thief;

/// <summary>
/// Marker component for thief hideout locations that scan nearby stolen items and
/// associate them with a specific thief mind while tracking the hideout's maximum score.
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
