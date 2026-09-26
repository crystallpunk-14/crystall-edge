namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Marks an entity as blocking the Light Monolith's charge while it exists within the Lucson
/// Sphere's murk radius (projected across z-levels) - see
/// <c>Content.Server._CE.MurkSphere.CEMurkSphereChargingBlockerSystem</c>. The entity's own name is
/// shown as the blocker's title in the Light Monolith monitor console.
/// </summary>
[RegisterComponent]
public sealed partial class CEMurkSphereChargingBlockerComponent : Component;
