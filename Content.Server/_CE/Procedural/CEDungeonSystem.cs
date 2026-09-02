using Content.Server._CE.ZLevels.Core;
using Content.Server.Decals;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural;

/// <summary>
/// Spawns <see cref="Content.Shared._CE.Procedural.CEDungeonRoom3DPrototype"/> room prefabs — atlas
/// rectangles stacked across one file per z-level — onto a station's z-network. Ported from
/// CrystallEdgeRogue's dungeon generator, stripped down to just the room-placement core (no
/// corridor/passway-connected dungeon pipeline).
/// </summary>
public sealed partial class CEDungeonSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEZLevelsSystem _zLevels = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private readonly List<(Vector2i, Tile)> _tiles = new();

    public static readonly ProtoId<ContentTileDefinition> FallbackTileId = "CEStone";

    public override void Initialize()
    {
        base.Initialize();

        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }
}
