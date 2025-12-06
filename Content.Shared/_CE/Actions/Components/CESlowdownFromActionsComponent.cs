using Robust.Shared.GameStates;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// apply slowdown effect from casting spells
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedActionSystem))]
public sealed partial class CESlowdownFromActionsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, float> SpeedAffectors = new();
}
