using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Ambitions.Components;

/// <summary>
/// Creates ambitious goals for the character and allows them to be rerolled a certain number of times, or until the allotted time runs out.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CESharedAmbitionsSystem))]
public sealed partial class CEAmbitionsSetupComponent : Component
{
    [DataField]
    public TimeSpan AvailableTime = TimeSpan.FromMinutes(5);

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public int RerollAmount = 10;

    /// <summary>
    /// How much ambition is generated when creating a character?
    /// </summary>
    [DataField]
    public int MaxAmbitions = 3;

    [DataField]
    public EntProtoId ToggleUiAction = "CEActionAmbitionsView";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleUiActionEntity;
}
