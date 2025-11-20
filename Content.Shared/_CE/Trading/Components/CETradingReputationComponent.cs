using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedTradingPlatformSystem))]
public sealed partial class CETradingReputationComponent : Component
{
    /// <summary>
    /// factions is unlocked for that player.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CETradingFactionPrototype>> Factions = new();
}
