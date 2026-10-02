using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EnergyScanner;

/// <summary>
/// Marker for clothing that grants energy scanner vision while worn in the eyes slot: a pipe overlay across z-levels
/// and detailed power network readings on examine. The wearer receives <see cref="CEEnergyScannerViewerComponent"/>.
/// Also holds the overlay mode, so it is kept with the glasses when they change hands.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEEnergyScannerClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public CEEnergyScannerMode Mode = CEEnergyScannerMode.Below;

    /// <summary>
    /// Action granted to the wearer that cycles <see cref="Mode"/>.
    /// </summary>
    [DataField]
    public EntProtoId Action = "CEActionEnergyScannerMode";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
