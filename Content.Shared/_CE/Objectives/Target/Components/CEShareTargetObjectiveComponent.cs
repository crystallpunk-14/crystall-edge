using Content.Shared.Whitelist;

namespace Content.Shared._CE.Objectives.Target.Components;

/// <summary>
/// Variant of <see cref="CETargetObjectiveComponent"/> that takes its target from another
/// <see cref="CETargetObjectiveComponent"/> objective on the same holder, instead of picking its own.
/// </summary>
[RegisterComponent]
[Access(typeof(CEShareTargetObjectiveSystem))]
public sealed partial class CEShareTargetObjectiveComponent : Component
{
    /// <summary>
    /// Whitelist that determines which of the holder's other objectives to share the target from.
    /// </summary>
    [DataField]
    public EntityWhitelist ObjectiveWhitelist = new();
}
