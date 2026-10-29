namespace Content.Server._CE.RoundPortal;

/// <summary>
/// Marks a mind whose player left the round through a <see cref="Content.Shared._CE.RoundPortal.CERoundPortalComponent"/>.
/// The body stays alive on a paused map, so objectives keep treating it as alive.
/// </summary>
[RegisterComponent, Access(typeof(CERoundPortalSystem))]
public sealed partial class CERoundLeavedComponent : Component;
