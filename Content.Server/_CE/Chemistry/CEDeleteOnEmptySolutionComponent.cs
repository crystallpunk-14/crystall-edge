using Content.Shared.Chemistry.Components;

namespace Content.Server._CE.Chemistry;

/// <summary>
/// Deletes this entity once the named <see cref="SolutionComponent"/> on it runs out of reagent.
/// </summary>
[RegisterComponent]
[Access(typeof(CEDeleteOnEmptySolutionSystem))]
public sealed partial class CEDeleteOnEmptySolutionComponent : Component
{
    [DataField(required: true)]
    public string SolutionId = SolutionComponent.DefaultSolutionId;
}
