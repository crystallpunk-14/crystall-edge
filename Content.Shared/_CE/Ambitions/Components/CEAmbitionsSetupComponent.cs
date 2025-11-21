using Content.Shared.Objectives;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Ambitions.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CESharedAmbitionsSystem))]
public sealed partial class CEAmbitionsSetupComponent : Component
{
    [DataField]
    public TimeSpan AvailableTime = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public int RerollAmount = 10;

    [DataField]
    public int MaxAmbitions = 3;
}
