using Content.Shared._CE.GOAP.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.GOAP.Sensors;

/// <summary>
/// Data-only base for a sensor entry: which condition key it writes and which target slot it inspects.
/// Concrete entries and their sensor components/systems live on the server.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class CEGOAPSensorEntryBase
{
    [DataField(required: true)]
    public ProtoId<CEGOAPConditionPrototype> ConditionKey;

    /// <summary>
    /// Target slot this sensor inspects, resolved through the agent's
    /// <see cref="Components.CEGOAPComponent.Targets"/>. Null for sensors that only look at the agent
    /// or the world around it.
    /// </summary>
    [DataField]
    public ProtoId<CEGOAPTargetPrototype>? Target;

    /// <summary>
    /// Attaches this entry to the agent: ensures the matching sensor component exists and appends
    /// the entry to it. Used when expanding a <see cref="CEGOAPBehaviorPrototype"/> onto a mob.
    /// </summary>
    public abstract void AddTo(EntityUid uid, IEntityManager entMan);
}
