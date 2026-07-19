using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Tracks which map coordinates this entity has researched, independently for each science area,
/// and how many research points it currently has to spend on research actions. Both fields are
/// networked so the owning client always has this locally (no extra BUI round-trip needed to read
/// it), and so <see cref="Content.Shared._CE.Workbench.Requirements.ResearchPointResource"/> can
/// be checked client-side (e.g. for live workbench recipe filtering).
/// </summary>
/// <remarks>
/// Whoever mutates <see cref="Researched"/> or <see cref="Points"/> must call
/// <c>Dirty(uid, component)</c> afterwards - AutoNetworkedField only controls how the
/// auto-generated state is (de)serialized, it does not detect field writes on its own.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEScienceResearchDataComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEScienceAreaPrototype>, HashSet<Vector2i>> Researched = new();

    [DataField, AutoNetworkedField]
    public int Points = 10;
}
