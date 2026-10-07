using Content.Shared._CE.Objectives.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Objectives.Target.Components;

/// <summary>
/// General component that manages objectives which target a given entity - picked from candidates
/// gathered via <see cref="CEGetObjectiveTargetCandidatesEvent"/> and filtered by
/// <see cref="CEValidateObjectiveTargetCandidateEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CETargetObjectiveSystem))]
public sealed partial class CETargetObjectiveComponent : Component
{
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// Set once the target left the round for good - its mind went to a ghost or nowhere at all, or
    /// its body got deleted. <see cref="Target"/> is cleared then, but the objective is never
    /// retargeted, and conditions treat it as their final outcome (killed, not survived, ...).
    /// </summary>
    [DataField]
    public bool TargetLost;

    /// <summary>
    /// Locale id for the objective title. It is passed "targetName" and "job" arguments.
    /// </summary>
    [DataField]
    public LocId? Title;
}

/// <summary>
/// Event raised on an objective to gather all potential target candidates. No filtering happens here.
/// </summary>
[ByRefEvent]
public record struct CEGetObjectiveTargetCandidatesEvent(Entity<CEObjectiveHolderComponent> Holder, List<EntityUid> Candidates);

/// <summary>
/// Event raised on an objective entity to check if a given candidate is valid as its target.
/// </summary>
[ByRefEvent]
public record struct CEValidateObjectiveTargetCandidateEvent(Entity<CEObjectiveHolderComponent> Holder, EntityUid Candidate)
{
    public readonly Entity<CEObjectiveHolderComponent> Holder = Holder;
    public readonly EntityUid Candidate = Candidate;
    public bool Valid { get; private set; } = true;

    public void Invalidate()
    {
        Valid = false;
    }
}

/// <summary>
/// Raised on a <see cref="CETargetObjectiveComponent"/> when its target changes.
/// </summary>
[ByRefEvent]
public readonly record struct CEObjectiveTargetChangedEvent(EntityUid? OldTarget, EntityUid? NewTarget);
