using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntitySlots;

/// <summary>
/// Generic entity-target action that creates one configured entity in a free fixed slot on the target.
/// </summary>
public sealed partial class CECreateEntityInFixedSlotActionEvent : EntityTargetActionEvent
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}

/// <summary>
/// Extension point raised on the performer before the configured product is spawned.
/// Domain adapters may replace the prototype or cancel the transaction.
/// </summary>
[ByRefEvent]
public record struct CEFixedSlotEntityCreatingEvent
{
    public EntityUid Target;
    public EntProtoId Prototype;
    public bool Cancelled;

    public CEFixedSlotEntityCreatingEvent(EntityUid target, EntProtoId prototype)
    {
        Target = target;
        Prototype = prototype;
    }
}

/// <summary>
/// Extension point raised on the performer after insertion but before the action is accepted.
/// Cancellation removes the created entity and leaves the action unhandled.
/// </summary>
[ByRefEvent]
public record struct CEFixedSlotEntityCreatedEvent(EntityUid Target, EntityUid Product, EntProtoId Prototype)
{
    public bool Cancelled;
}
