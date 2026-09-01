using System.Numerics;

namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Owns authored presentation transforms for a stable collection of standard container slots.
/// </summary>
[RegisterComponent]
[Access(typeof(CEFixedEntitySlotSystem), Other = AccessPermissions.Read)]
public sealed partial class CEFixedEntitySlotsComponent : Component
{
    /// <summary>
    /// Stable slot definitions. Their list indices are persistent slot identifiers.
    /// </summary>
    [DataField(required: true)]
    public List<CEFixedEntitySlotDefinition> Slots = new();

}

/// <summary>
/// One authored sprite-space presentation transform in a <see cref="CEFixedEntitySlotsComponent"/>.
/// </summary>
[DataDefinition]
public sealed partial class CEFixedEntitySlotDefinition
{
    [DataField(required: true)]
    public Vector2 Offset;

    [DataField]
    public Angle Rotation;
}

/// <summary>
/// Raised on an occupant after it has been placed into a fixed entity slot.
/// </summary>
[ByRefEvent]
public readonly record struct CEFixedEntitySlotInsertedEvent(EntityUid Host, int Slot);

/// <summary>
/// Raised on an occupant after it has stopped occupying a fixed entity slot.
/// </summary>
[ByRefEvent]
public readonly record struct CEFixedEntitySlotRemovedEvent(EntityUid Host, int Slot);
