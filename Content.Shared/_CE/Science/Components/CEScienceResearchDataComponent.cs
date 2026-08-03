using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Tracks which map coordinates this entity has researched, independently for each science area,
/// and how many research points of each essence type it currently has to spend on research
/// actions. Whether a given achievement has been discovered is tracked by
/// <see cref="Content.Shared._CE.Knowledge.Components.CEKnowledgeComponent"/> instead - an
/// achievement cell only shows its icon in full colour once its linked knowledge is known.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CEScienceResearchDataComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEScienceAreaPrototype>, HashSet<Vector2i>> Researched = new();

    /// <summary>
    /// Research points currently held, keyed by essence type - one balance per aspect of
    /// thaumaturgical essence, rather than a single generic currency.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Points = new();
}
