using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Components;

/// <summary>
/// Reflects the entity's level of reputation, debts, and balance sheet in the “outside” world.
/// Used for personal progression in trading systems
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedTradingPlatformSystem))]
public sealed partial class CETradingReputationComponent : Component
{
    /// <summary>
    /// is both a reputation counter for each faction and an indicator of whether that faction is unlocked for that player.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CETradingFactionPrototype>, FixedPoint2> Reputation = new();
}
