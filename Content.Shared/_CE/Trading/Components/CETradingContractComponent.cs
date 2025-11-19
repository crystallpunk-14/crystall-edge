using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Components;

[RegisterComponent, Access(typeof(CESharedTradingPlatformSystem))]
public sealed partial class CETradingContractComponent : Component
{
    [DataField]
    public ProtoId<CETradingFactionPrototype> Faction;
}
