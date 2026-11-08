namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Deletes the entity when dawn begins on the main map. Works for status effect entities too, ending the effect.
/// A plain deletion: no despawn or destruction reactions are triggered.
/// </summary>
[RegisterComponent]
public sealed partial class CEDeleteOnDawnComponent : Component;
