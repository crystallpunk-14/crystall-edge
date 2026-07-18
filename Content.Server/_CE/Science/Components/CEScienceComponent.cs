using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science.Components;

/// <summary>
/// Marks the singleton nullspace entity holding round-wide science data.
/// Spawned by <see cref="CEScienceSystem"/> on round start; duplicates are deleted on MapInit.
/// </summary>
[RegisterComponent]
public sealed partial class CEScienceComponent : Component
{
    /// <summary>
    /// Each science area's research map: only non-empty cells (dead zones, achievements, etc.)
    /// are stored, keyed by their coordinate on that area's independent map.
    /// </summary>
    public Dictionary<ProtoId<CEScienceAreaPrototype>, Dictionary<Vector2i, CEScienceMapCell>> Areas = new();
}
