using System.Linq;
using Content.Server._CE.ZLevels.Core;
using Content.Server.Station.Systems;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._CE.MagicEssence.Systems;

/// <summary>
/// Generic magic essence node spawning utilities. Doesn't own any pool/lifetime management itself -
/// see <see cref="CEMagicEssenceNodeRuleSystem"/> for the round's maintained node pool.
/// </summary>
public sealed partial class CEMagicEssenceNodeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private CEZLevelsSystem _zLevels = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    private readonly EntProtoId _magicEssenceNodeEntity = "CEMagicEssenceNode";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicEssenceNodeComponent, MapInitEvent>(OnNodeMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMagicEssenceNodeComponent>();
        while (query.MoveNext(out var uid, out var node))
        {
            if (_timing.CurTime < node.NextGenerationTime)
                continue;

            node.NextGenerationTime = _timing.CurTime + node.GenerationInterval;
            GenerateEssence((uid, node));
        }
    }

    /// <summary>
    /// Rolls 3 random essence aspects for a freshly spawned node, and rolls its total lifetime
    /// between <see cref="CEMagicEssenceNodeComponent.MinLifetime"/> and
    /// <see cref="CEMagicEssenceNodeComponent.MaxLifetime"/>, applying it to both the networked
    /// <see cref="CEMagicEssenceNodeComponent.Lifetime"/> (for the client's fade curve) and the
    /// entity's own <see cref="TimedDespawnComponent"/> (the actual despawn timer). Aspects may repeat.
    /// </summary>
    private void OnNodeMapInit(Entity<CEMagicEssenceNodeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EssenceA = _essence.GetRandomEssenceType();
        ent.Comp.EssenceB = _essence.GetRandomEssenceType();
        ent.Comp.EssenceC = _essence.GetRandomEssenceType();
        ent.Comp.NextGenerationTime = _timing.CurTime + ent.Comp.GenerationInterval;

        var minSeconds = (float)ent.Comp.MinLifetime.TotalSeconds;
        var maxSeconds = (float)ent.Comp.MaxLifetime.TotalSeconds;
        var lifetime = TimeSpan.FromSeconds(_random.NextFloat(minSeconds, maxSeconds));

        ent.Comp.SpawnTime = _timing.CurTime;
        ent.Comp.Lifetime = lifetime;

        if (TryComp<TimedDespawnComponent>(ent, out var despawn))
            despawn.Lifetime = (float)lifetime.TotalSeconds;

        Dirty(ent);
    }

    /// <summary>
    /// Generates 1u of essence reagent for one of the node's 3 rolled aspects (70% <see cref="CEMagicEssenceNodeComponent.EssenceA"/> /
    /// 20% <see cref="CEMagicEssenceNodeComponent.EssenceB"/> / 10% <see cref="CEMagicEssenceNodeComponent.EssenceC"/>), adding it to the
    /// node's own solution. If the solution has no room left, the essence is spilled into the air as a
    /// floating pickup entity instead - see <see cref="CEUnpoweredEssenceLeakSystem"/> for the same pattern.
    /// </summary>
    private void GenerateEssence(Entity<CEMagicEssenceNodeComponent> ent)
    {
        if (PickWeightedSlot(ent.Comp) is not { } essenceId || !_proto.TryIndex(essenceId, out var essenceType))
            return;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        if (solution.AvailableVolume < FixedPoint2.New(1))
        {
            if (essenceType.EssenceProto is { } essenceProto)
                Spawn(essenceProto, Transform(ent).Coordinates);

            return;
        }

        if (essenceType.Reagent is { } reagent)
            _solutionContainer.TryAddReagent(ent.Comp.Solution.Value, reagent, FixedPoint2.New(1), out _);
    }

    private ProtoId<CEMagicEssenceTypePrototype>? PickWeightedSlot(CEMagicEssenceNodeComponent node)
    {
        var roll = _random.NextFloat();
        if (roll < 0.7f)
            return node.EssenceA;
        if (roll < 0.9f)
            return node.EssenceB;
        return node.EssenceC;
    }

    /// <summary>
    /// Finds a random valid spot in <paramref name="network"/> and spawns a magic essence node there.
    /// Returns null if no valid spot could be found.
    /// </summary>
    public EntityUid? SpawnNodeInNetwork(Entity<CEZGridNetworkComponent> network)
    {
        if (!TryGetRandomNodeLocation(network, out var coordinates))
            return null;

        return Spawn(_magicEssenceNodeEntity, coordinates);
    }

    /// <inheritdoc cref="SpawnNodeInNetwork(Robust.Shared.GameObjects.Entity{Content.Shared._CE.ZLevels.Core.Components.CEZGridNetworkComponent})"/>
    public EntityUid? SpawnNodeInNetwork(Entity<CEZMapNetworkComponent> network)
    {
        if (!TryGetRandomNodeLocation(network, out var coordinates))
            return null;

        return Spawn(_magicEssenceNodeEntity, coordinates);
    }

    /// <summary>
    /// Resolves the z-map network of the first found station's largest grid - currently all maps
    /// are planetary (the grid entity is itself the map entity), so this is a map-network lookup
    /// rather than a grid-network one.
    /// </summary>
    public bool TryGetStationNetwork(out Entity<CEZMapNetworkComponent> network)
    {
        network = default;

        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        if (!stationQuery.MoveNext(out var stationUid, out var stationData))
        {
            Log.Warning("CEMagicEssenceNodeSystem: no StationDataComponent found in the world.");
            return false;
        }

        if (_stations.GetLargestGrid((stationUid, stationData)) is not { } grid)
        {
            Log.Warning($"CEMagicEssenceNodeSystem: station {ToPrettyString(stationUid)} has no grids.");
            return false;
        }

        if (!_zLevels.TryGetMapNetwork(grid, out network))
        {
            Log.Warning($"CEMagicEssenceNodeSystem: grid {ToPrettyString(grid)} (station {ToPrettyString(stationUid)}) isn't part of a z-map network.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to find a random tile suitable for spawning a magic essence node, picking randomly among
    /// every grid (i.e. every z-level floor) belonging to the given z-grid network. Rejects tiles
    /// occupied by a static, hard, impassable anchored entity (walls, etc).
    /// </summary>
    public bool TryGetRandomNodeLocation(Entity<CEZGridNetworkComponent> network, out EntityCoordinates coordinates)
    {
        return TryGetRandomTileOnGrids(network.Comp.Grids.ToList(), out coordinates);
    }

    /// <summary>
    /// Tries to find a random tile suitable for spawning a magic essence node, scanning every map in
    /// the given z-map network but only considering planet maps - ones where the map entity is
    /// itself the grid entity, as opposed to regular multi-grid station maps.
    /// </summary>
    public bool TryGetRandomNodeLocation(Entity<CEZMapNetworkComponent> network, out EntityCoordinates coordinates)
    {
        var planetMaps = new List<EntityUid>();
        foreach (var mapUid in network.Comp.SortedZLevels)
        {
            if (mapUid != EntityUid.Invalid && HasComp<MapGridComponent>(mapUid))
                planetMaps.Add(mapUid);
        }

        return TryGetRandomTileOnGrids(planetMaps, out coordinates);
    }

    private bool TryGetRandomTileOnGrids(List<EntityUid> grids, out EntityCoordinates coordinates)
    {
        coordinates = default;

        if (grids.Count == 0)
            return false;

        _random.Shuffle(grids);

        foreach (var grid in grids)
        {
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                continue;

            if (TryGetRandomTileOnGrid(grid, gridComp, out coordinates))
                return true;
        }

        return false;
    }

    private bool TryGetRandomTileOnGrid(EntityUid grid, MapGridComponent gridComp, out EntityCoordinates coordinates)
    {
        coordinates = default;

        for (var i = 0; i < 25; i++)
        {
            if (!TryPickRandomTile(grid, gridComp, out var tile))
                return false; // grid has no tiles at all - no point retrying

            var valid = true;
            foreach (var ent in _mapSystem.GetAnchoredEntities(grid, gridComp, tile))
            {
                if (!_physicsQuery.TryGetComponent(ent, out var body))
                    continue;
                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }

            if (!valid)
                continue;

            coordinates = _mapSystem.GridTileToLocal(grid, gridComp, tile);
            return true;
        }

        return false;
    }

    private bool TryPickRandomTile(EntityUid grid, MapGridComponent gridComp, out Vector2i tile)
    {
        tile = default;
        var found = false;
        var seen = 0;

        foreach (var tileRef in _mapSystem.GetAllTiles(grid, gridComp))
        {
            seen++;
            if (_random.Next(seen) != 0)
                continue;

            tile = tileRef.GridIndices;
            found = true;
        }

        return found;
    }
}
