using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;
using SharedEntityEffect = Content.Shared.EntityEffects.EntityEffect;

namespace Content.Server._CE.Production;

/// <summary>
/// Adds a charge to one prototype-authored action after a conditional production interval.
/// The action and <see cref="Content.Shared.Charges.Components.LimitedChargesComponent"/> remain
/// the authoritative owners of the pending discrete output.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEProductionAccumulatorComponent : Component
{
    /// <summary>
    /// Granted action whose charge represents one pending production output.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId OutputAction;

    /// <summary>
    /// Conditions evaluated on the producer before input costs are applied.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityCondition[] Conditions = [];

    /// <summary>
    /// Optional canonical entity effect applied to the producer before an output charge is added.
    /// A single effect keeps the operation transactional: a failed effect never creates output.
    /// </summary>
    [DataField]
    public SharedEntityEffect? InputCost;

    /// <summary>
    /// Minimum interval between prerequisite/action/charge checks while production is due or pending.
    /// </summary>
    [DataField]
    public TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    [DataField(required: true)]
    public TimeSpan FirstMinimum;

    [DataField(required: true)]
    public TimeSpan FirstMaximum;

    [DataField(required: true)]
    public TimeSpan RepeatMinimum;

    [DataField(required: true)]
    public TimeSpan RepeatMaximum;

    [DataField, AutoPausedField]
    public TimeSpan NextProductionAt;

    [DataField, AutoPausedField]
    public TimeSpan NextPollAt;

    /// <summary>
    /// Whether the next interval is waiting for the previously added action charge to be spent.
    /// </summary>
    [DataField, ViewVariables]
    public bool WaitingForOutputSpend;

    /// <summary>
    /// Stops an invalid accumulator without placing an overflow-prone sentinel in paused timestamps.
    /// </summary>
    [DataField, ViewVariables]
    public bool Disabled;
}
