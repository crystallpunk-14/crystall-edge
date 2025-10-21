using Content.Server._CE.ZLevels.Commands;
using Content.Server._CE.ZLevels.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._CE.ZLevels.Components;

/// <summary>
/// Initializes the z-level system by creating a series of linked maps
/// </summary>
[RegisterComponent, Access(typeof(CEStationZLevelsSystem), typeof(CECombineMapsIntoZLevelsCommand))]
public sealed partial class CEStationZLevelsComponent : Component
{
    [DataField(required: true)]
    public int DefaultMapLevel = 0;

    [DataField(required: true)]
    public Dictionary<int, CEZLevelEntry> Levels = new();

    public bool Initialized = false;

    public Dictionary<MapId, int> LevelEntities = new();
}

[DataRecord, Serializable]
public sealed class CEZLevelEntry
{
    public ResPath? Path { get; set; } = null;
}
