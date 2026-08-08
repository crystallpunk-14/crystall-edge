using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CEScienceResearchDataComponent : Component
{
    /// <summary>
    /// Research points currently held
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Points = new();

    /// <summary>
    /// Discovery prototypes not currently sitting in this player's own unresolved research draft,
    /// drawable when rolling a new offer for them. Personal to this player - not networked, the
    /// client never needs to see it.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<CEScienceDiscoveryPrototype>> AvailableDiscoveries = new();
}
