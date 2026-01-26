using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Pulling;

/// <summary>
/// Temporary component added during z-level transitions to track the entity pulling this entity across z-levels.
/// </summary>
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class CEZLevelActivePulledComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid PullerEnt;
}
