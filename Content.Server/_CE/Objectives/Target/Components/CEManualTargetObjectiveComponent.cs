using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Lets the holder of a target objective pick the target themselves for a limited time, using an action
/// on a character. A random target is picked as usual once the time runs out.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CEManualTargetObjectiveSystem))]
public sealed partial class CEManualTargetObjectiveComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Action given to the holder while they can pick, raising a CESelectManualTargetActionEvent.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action;

    /// <summary>
    /// Status effect on the holder's body showing how much time is left to pick.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    /// <summary>
    /// Sent to the holder once the target is decided, either way. Passed a "targetName" argument.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// The time to pick has run out, so random selection is no longer held back.
    /// </summary>
    [DataField]
    public bool Expired;

    /// <summary>
    /// A target has been decided, nothing left to do.
    /// </summary>
    [DataField]
    public bool Finished;

    /// <summary>
    /// The holder's mind.
    /// </summary>
    [DataField]
    public EntityUid? Holder;

    /// <summary>
    /// Body currently showing the action and the status effect.
    /// </summary>
    [DataField]
    public EntityUid? Body;

    [DataField]
    public EntityUid? ActionEntity;
}
