using Robust.Shared.Serialization;

namespace Content.Shared._CE.EntitySlots;

/// <summary>
/// Networked appearance data for presenting a contained entity in an authored fixed slot.
/// Physical transforms remain at the canonical container-local origin.
/// </summary>
[Serializable, NetSerializable]
public enum CEFixedSlotVisuals : byte
{
    Active,
    Offset,
    Rotation,
}
