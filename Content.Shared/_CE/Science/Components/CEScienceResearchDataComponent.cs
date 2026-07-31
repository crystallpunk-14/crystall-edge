using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Tracks which map coordinates this entity has researched, independently for each science area,
/// how many research points of each essence type it currently has to spend on research actions,
/// and which achievements it has actually discovered (as opposed to merely revealed on the map).
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

    /// <summary>
    /// Achievements this entity has completed the "discover achievement" research action for.
    /// A revealed (researched) achievement cell only shows its icon in full colour once its
    /// achievement is in this set - otherwise it's drawn tinted black.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CEScienceAchievementPrototype>> DiscoveredAchievements = new();
}
