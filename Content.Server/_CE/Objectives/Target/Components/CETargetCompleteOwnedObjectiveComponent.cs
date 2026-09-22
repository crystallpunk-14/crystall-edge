using Content.Server._CE.Objectives.Target;
using Content.Shared.Whitelist;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Objective condition that succeeds once the objective's target mind completes all of its own
/// personal objectives (or fails, if <see cref="Invert"/>). Also used as the filter that keeps a
/// <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/> from
/// picking a target that doesn't have any personal objectives to begin with.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetCompleteObjectivesSystem))]
public sealed partial class CETargetCompleteOwnedObjectiveComponent : Component
{
    /// <summary>
    /// The mind of our target objective's target. Stored separately (rather than following the
    /// target objective's target entity) so we keep tracking the same mind's objectives even if that
    /// mind swaps bodies.
    /// </summary>
    [DataField]
    public EntityUid? TargetMind;

    /// <summary>
    /// Objectives blacklisted and ignored for the purposes of this condition - used to prevent
    /// infinite loops, e.g. a lover targeting another lover.
    /// </summary>
    [DataField]
    public EntityWhitelist? ObjectiveBlacklist;

    [DataField]
    public float DefaultProgress;

    /// <summary>
    /// If true, inverts progress - completing objectives makes it go down instead of up.
    /// </summary>
    [DataField]
    public bool Invert;
}
