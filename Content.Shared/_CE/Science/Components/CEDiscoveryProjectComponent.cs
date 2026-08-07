using Content.Shared._CE.Science;
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
    public ProtoId<CEScienceDiscoveryPrototype>? Discovery;

    /// <summary>
    /// The generated puzzle map. Sparse: a coordinate absent from this dictionary is open ground,
    /// free for any player at the table to place an aspect on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, CEResearchMapTile> Tiles = new();
}
