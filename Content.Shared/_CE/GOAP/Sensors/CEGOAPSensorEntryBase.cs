using Content.Shared._CE.GOAP.Selectors;
using Content.Shared._CE.GOAP.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.GOAP.Sensors;

/// <summary>
/// Data-only base for a sensor entry: which condition key it writes and which target it inspects.
/// Concrete entries and their sensor components/systems live on the server.
/// </summary>
[DataDefinition]
public abstract partial class CEGOAPSensorEntryBase
{
    [DataField(required: true)]
    public ProtoId<CEGOAPConditionPrototype> ConditionKey;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;

    /// <summary>
    /// Attaches this entry to the agent: ensures the matching sensor component exists and appends
    /// the entry to it. Used when expanding a <see cref="CEGOAPBehaviorPrototype"/> onto a mob.
    /// </summary>
    public abstract void AddTo(EntityUid uid, IEntityManager entMan);
}
