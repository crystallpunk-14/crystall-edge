using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Initializes the z-level system by creating a series of linked maps
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedZLevelsSystem))]
public sealed partial class CEZLevelsComponent : Component
{
    //public bool ZLevelsInitialized = false;

    //[DataField(required: true)]
    //public int DefaultMapLevel = 0;

    /// <summary>
    /// Used for roundstart zLevel network generation
    /// </summary>
    //[DataField(required: true)]
    //public Dictionary<int, CEZLevelEntry> Levels = new();

    [DataField, AutoNetworkedField]
    public Dictionary<MapId, int> ZLevels = new();
}

[DataRecord, Serializable]
public sealed class CEZLevelEntry
{
    public ResPath? Path { get; set; } = null;
}
