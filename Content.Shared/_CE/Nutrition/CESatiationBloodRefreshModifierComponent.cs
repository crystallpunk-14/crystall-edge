using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Nutrition;

/// <summary>
/// Scales natural blood regeneration of its entity by the entity's current satiations.
/// Multipliers of all listed satiation types are multiplied together.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true),
 Access(typeof(CESatiationBloodRefreshModifierSystem))]
public sealed partial class CESatiationBloodRefreshModifierComponent : Component
{
    /// <summary>
    /// Blood refresh multipliers contained within <see cref="SatiationThresholds{T}"/>,
    /// keyed by the <see cref="SatiationTypePrototype">satiation types</see> they are thresholds for.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<float>> Satiations = [];
}
