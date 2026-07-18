using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

/// <summary>
/// A single non-empty cell of a science area's research map.
/// Empty cells are not stored anywhere - only cells with actual content, like dead zones or achievements.
/// Extend with new sealed subclasses to add new cell types with their own data.
/// </summary>
[Serializable, NetSerializable]
public abstract class CEScienceMapCell;

[Serializable, NetSerializable]
public sealed class CEScienceDeadZoneCell : CEScienceMapCell;

[Serializable, NetSerializable]
public sealed class CEScienceAchievementCell(ProtoId<CEScienceAchievementPrototype> achievement) : CEScienceMapCell
{
    public readonly ProtoId<CEScienceAchievementPrototype> Achievement = achievement;
}
