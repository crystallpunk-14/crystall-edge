namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Supports tiles on the map above
/// </summary>
[RegisterComponent]
public sealed partial class CEMapSupportComponent : Component
{
    [DataField]
    public int SupportStrength = 10;
}
