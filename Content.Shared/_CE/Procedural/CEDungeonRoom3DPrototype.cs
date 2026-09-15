using Content.Shared._CE.ZLevels.Mapping.Prototypes;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Procedural;

/// <summary>
/// ZLevels 3D alternative for DungeonRoomPrototype
/// </summary>
[Prototype("dungeonRoom3d")]
public sealed partial class CEDungeonRoom3DPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public Vector2i Size;

    [DataField(required: true)]
    public int Height = 1;

    /// <summary>
    /// Path to the file to use for the room.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CEZLevelMapPrototype> ZLevelMap;

    /// <summary>
    /// Tile offset into the atlas to use for the room.
    /// </summary>
    [DataField(required: true)]
    public Vector2i Offset;

    /// <summary>
    /// These tiles will be ignored when copying from the atlas into the actual game,
    /// allowing you to make rooms of irregular shapes that blend seamlessly into their surroundings
    /// </summary>
    [DataField]
    public ProtoId<ContentTileDefinition>? IgnoreTile;

    /// <summary>
    /// Selection weight for this room. Higher values make the room appear more frequently.
    /// </summary>
    [DataField]
    public float Weight = 1f;
}
