namespace Content.Shared._CE.RoundPortal;

/// <summary>
/// Marks a mind whose player left the round through a <see cref="CERoundPortalComponent"/>.
/// The body stays alive on a paused map - see <see cref="Content.Shared._CE.Character.CEBodyState.LeftRound"/>
/// for how objectives treat it.
/// </summary>
[RegisterComponent]
public sealed partial class CERoundLeavedComponent : Component;
