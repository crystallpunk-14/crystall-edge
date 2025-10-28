using Content.Server._CE.ZLevels.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server._CE.ZLevels.Components;

/// <summary>
/// Initializes the z-level system by creating a series of linked maps
/// </summary>
[RegisterComponent, Access(typeof(CEZLevelsSystem))]
public sealed partial class CEStationZLevelsComponent : Component
{
    public bool ZLevelsInitialized = false;

    [DataField(required: true)]
    public int DefaultMapLevel = 0;

    /// <summary>
    /// Used for roundstart zLevel network generation
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, CEZLevelEntry> Levels = new();
}

[DataRecord, Serializable]
public sealed class CEZLevelEntry
{
    public ResPath? Path { get; set; } = null;
}
