using Content.Shared._CE.Objectives;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Objectives.Components;

/// <summary>
/// Denotes an entity (a mind, in practice) that can hold <see cref="CEObjectiveComponent"/>
/// objectives. Multiple holders can reference the same objective entity - used for objectives
/// shared by a whole secret department, where every member points at the same objective instead
/// of getting a personal copy.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(CESharedObjectiveSystem))]
public sealed partial class CEObjectiveHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Objectives = new();
}

/// <summary>
/// Raised on a <see cref="CEObjectiveHolderComponent"/> when its objective list changes.
/// </summary>
[ByRefEvent]
public readonly record struct CEObjectivesChangedEvent(EntityUid Holder);

/// <summary>
/// Raised on a CE objective when its progress changes.
/// </summary>
[ByRefEvent]
public readonly record struct CEObjectiveProgressChangedEvent(Entity<CEObjectiveComponent> Objective, float OldProgress, float NewProgress);
