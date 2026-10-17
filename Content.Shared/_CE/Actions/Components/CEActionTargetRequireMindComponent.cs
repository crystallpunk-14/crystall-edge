namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Blocks an entity-target action when the target body has no mind in it.
/// The check runs during action validation, so a blocked cast consumes no cooldown.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionTargetRequireMindComponent : Component
{
}
