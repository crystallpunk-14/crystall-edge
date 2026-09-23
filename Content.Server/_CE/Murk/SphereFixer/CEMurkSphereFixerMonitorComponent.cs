namespace Content.Server._CE.Murk.SphereFixer;

/// <summary>
/// Marks an entity as a Light Monolith monitor console, so
/// <see cref="CEMurkSphereFixerMonitorSystem"/> knows to push charge/blocker updates to it.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkSphereFixerMonitorSystem))]
public sealed partial class CEMurkSphereFixerMonitorComponent : Component;
