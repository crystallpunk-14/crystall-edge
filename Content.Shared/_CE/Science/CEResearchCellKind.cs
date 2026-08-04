using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

/// <summary>
/// Coarse classification of a research map cell's content, used to decide which
/// <see cref="Prototypes.CEResearchActionPrototype"/>s apply to it. Extend when a new
/// <see cref="CEScienceMapCell"/> subtype is added.
/// </summary>
[Serializable, NetSerializable]
[Flags]
public enum CEResearchCellKind
{
    None = 0,
    Empty = 1 << 0,
    DeadZone = 1 << 1,
    Discovery = 1 << 2,
    Star = 1 << 3,
    OfferedStar = 1 << 4,
}
