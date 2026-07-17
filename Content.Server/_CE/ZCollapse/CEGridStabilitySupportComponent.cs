namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Bridges structural stability between the Z-level this entity is anchored on and the Z-level
/// directly above it. Whichever side currently has a live tile at this entity's position feeds
/// <see cref="SupportStrength"/> as a seed into the other side. This is what lets structures hang
/// below a floating grid: place the support on the lower level, bridging up into the grid above.
/// </summary>
[RegisterComponent]
public sealed partial class CEGridStabilitySupportComponent : Component
{
    [DataField]
    public int SupportStrength = 10;
}
