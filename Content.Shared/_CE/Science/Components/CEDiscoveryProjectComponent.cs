using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks an active research project entity, resolved from a <see cref="CEUnselectedDiscoveryProjectComponent"/>
/// once its author chose one of the offered discoveries.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEDiscoveryProjectComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CEScienceDiscoveryPrototype> Discovery;
}
