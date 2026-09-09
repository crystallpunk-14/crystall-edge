using Robust.Shared.Serialization;

namespace Content.Shared._CE.Containers;

/// <summary>
/// Networked appearance data for presenting a contained entity in an authored fixed slot.
/// Physical transforms remain at the canonical container-local origin.
/// </summary>
[Serializable, NetSerializable]
public enum CEContainerSlotVisuals : byte
{
    Active,
    Offset,
    Rotation,
}
