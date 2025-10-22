using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._CE.ZLevels;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedZLevelsSystem))]
public sealed partial class CEZLevelsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<MapId, int> ZLevels = new();
}
