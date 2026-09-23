namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Marks an entity as blocking the Light Monolith's charge while it exists within the Lucson
/// Sphere's murk radius (projected across z-levels) - see
/// <c>Content.Server._CE.Murk.SphereFixer.CEMurkSphereChargingBlockerSystem</c>.
/// </summary>
[RegisterComponent]
public sealed partial class CEMurkSphereChargingBlockerComponent : Component
{
    /// <summary>
    /// Shown as the blocker's title in the Light Monolith monitor console.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}
