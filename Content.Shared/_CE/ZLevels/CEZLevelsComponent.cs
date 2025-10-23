using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Tracker that tracks all maps added to the zLevel network. Usually entity in Nullspace,
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedZLevelsSystem))]
public sealed partial class CEZLevelsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<MapId, int> ZLevels = new();
}
