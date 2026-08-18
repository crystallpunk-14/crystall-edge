namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Blocks a world- or entity-target action when the target is not within line of sight of the user.
/// Sight is broken by opaque occluders (walls) but not by transparent obstacles (windows).
/// The check runs during action validation, so a blocked cast consumes no cooldown.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionRequireLineOfSightComponent : Component
{
}
