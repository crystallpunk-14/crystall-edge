namespace Content.Server._CE.RoundPortal;

/// <summary>
/// Marks the paused map holding the bodies of players who left the round.
/// </summary>
[RegisterComponent, Access(typeof(CERoundPortalSystem))]
public sealed partial class CERoundLeavedStorageMapComponent : Component;
