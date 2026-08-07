using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks a research project entity that has an unresolved offer of discoveries - drawn candidates
/// waiting for <see cref="Player"/> to pick one.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEUnselectedDiscoveryProjectComponent : Component
{
    /// <summary>
    /// The player this offer was rolled for.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Player;

    /// <summary>
    /// The discoveries offered - pick one to resolve this project.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<CEScienceDiscoveryPrototype>> Candidates = new();
}
