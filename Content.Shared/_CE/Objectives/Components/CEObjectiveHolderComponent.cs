using Content.Shared._CE.Objectives;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Objectives.Components;

/// <summary>
/// Denotes an entity (a mind, in practice) that can hold <see cref="CEObjectiveComponent"/>
/// objectives.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(CESharedObjectiveSystem))]
public sealed partial class CEObjectiveHolderComponent : Component
{
    /// <summary>
    /// The complete set of objectives that can be viewed - <see cref="OwnedObjectives"/> plus
    /// whatever <see cref="CEGetAdditionalObjectivesEvent"/> adds from other sources (e.g. a
    /// secret department's shared objectives). Recomputed by
    /// <see cref="CESharedObjectiveSystem.RegenerateObjectiveList"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Objectives = new();

    /// <summary>
    /// Objectives created for and owned by this holder directly - the only ones it can remove via
    /// <see cref="CESharedObjectiveSystem.TryRemoveObjective"/>. Subset of <see cref="Objectives"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> OwnedObjectives = new();
}

/// <summary>
/// Raised on a <see cref="CEObjectiveHolderComponent"/> when its objective list changes.
/// </summary>
[ByRefEvent]
public readonly record struct CEObjectivesChangedEvent(EntityUid Holder);

/// <summary>
/// Raised on an objective holder to collect any additional objectives it has from other sources -
/// e.g. a secret department's shared objectives, owned by the department rather than the member.
/// </summary>
[ByRefEvent]
public record struct CEGetAdditionalObjectivesEvent(Entity<CEObjectiveHolderComponent> Holder, List<Entity<CEObjectiveComponent>> Objectives);

/// <summary>
/// Raised on a CE objective when its progress changes.
/// </summary>
[ByRefEvent]
public readonly record struct CEObjectiveProgressChangedEvent(Entity<CEObjectiveComponent> Objective, float OldProgress, float NewProgress);
