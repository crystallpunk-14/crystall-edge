using Content.Shared.Objectives;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Ambitions.Components;

/// <summary>
/// Food of the specified type can be transferred to this entity.
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

    [DataField, AutoNetworkedField]
    public List<ObjectiveInfo> Ambitions;
}
