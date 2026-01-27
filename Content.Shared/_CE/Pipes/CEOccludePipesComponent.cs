namespace Content.Shared._CE.Pipes;

/// <summary>
///     Marker component indicating that this entity should occlude pipes
///     (used for drawing entities behind walls with reduced depth)
/// </summary>
[RegisterComponent]
public sealed partial class CEOccludePipesComponent : Component
{
}
