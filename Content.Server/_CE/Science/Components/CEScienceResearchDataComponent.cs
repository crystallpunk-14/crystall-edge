using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science.Components;

/// <summary>
/// Tracks which map coordinates this entity has researched, independently for each science area.
/// </summary>
[RegisterComponent]
public sealed partial class CEScienceResearchDataComponent : Component
{
    public Dictionary<ProtoId<CEScienceAreaPrototype>, HashSet<Vector2i>> Researched = new();
}
