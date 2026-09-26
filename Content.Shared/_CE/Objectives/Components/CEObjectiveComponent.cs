using Content.Shared._CE.Objectives;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Objectives.Components;

/// <summary>
/// Denotes a CE objective associated with a <see cref="CEObjectiveHolderComponent"/>. Networked
/// directly to the holder's owning client (via a PVS session override) so the character menu can
/// read it straight off the entity, instead of going through a server request/response snapshot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(CESharedObjectiveSystem))]
[EntityCategory("Objectives")]
public sealed partial class CEObjectiveComponent : Component
{
    /// <summary>
    /// Current progress on the objective on the interval [0, 1].
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Progress;
}

/// <summary>
/// Event raised on a CE objective to calculate its current progress on the interval [0, 1].
/// </summary>
[ByRefEvent]
public record struct CEGetObjectiveProgressEvent(float Progress = 0);

/// <summary>
/// Event raised on a CE objective right after it's spawned and added to its holder, so condition
/// systems can set its title/description/icon.
/// </summary>
[ByRefEvent]
public readonly record struct CEInitializeObjectiveEvent(Entity<CEObjectiveHolderComponent> Holder);
