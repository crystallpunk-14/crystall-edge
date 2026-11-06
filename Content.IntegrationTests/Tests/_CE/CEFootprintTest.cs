#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Gravity;
using Content.Shared._CE.Footprints;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEFootprintTest
{
    private static readonly EntProtoId Human = "CEMobHuman";
    private static readonly EntProtoId Footprint = "CEFootprint";
    private static readonly ProtoId<ReagentPrototype> Blood = "Blood";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private const int Width = 8;

    /// <summary>
    /// A human walks through a blood puddle, carries the blood on the feet and leaves bloody footprints after it.
    /// </summary>
    [Test]
    public async Task WalkingThroughPuddleLeavesFootprints()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();

        var mapSystem = entMan.System<SharedMapSystem>();
        var xformSystem = entMan.System<SharedTransformSystem>();
        var puddleSystem = entMan.System<PuddleSystem>();

        EntityUid human = default;
        FixedPoint2 puddleVolumeBefore = default;

        await server.WaitPost(() =>
        {
            PreparePlatform(entMan, map.Grid, roofed: true);

            var blood = new Solution(Blood.Id, 20);
            puddleSystem.TrySpillAt(mapSystem.GridTileToLocal(map.Grid, map.Grid, new Vector2i(1, 0)), blood, out _, false);
            puddleVolumeBefore = GetPuddleVolume(entMan, map.Grid, new Vector2i(1, 0));

            human = entMan.SpawnEntity(Human, new EntityCoordinates(map.Grid, 0.5f, 0.5f));
        });

        await server.WaitPost(() =>
        {
            // Walk east in small steps, like real movement does.
            for (var x = 0.75f; x <= Width - 0.5f; x += 0.25f)
            {
                xformSystem.SetCoordinates(human, new EntityCoordinates(map.Grid, x, 0.5f));
            }
        });

        await server.WaitAssertion(() =>
        {
            var puddleVolumeAfter = GetPuddleVolume(entMan, map.Grid, new Vector2i(1, 0));
            Assert.That(puddleVolumeAfter, Is.LessThan(puddleVolumeBefore), "Stepping into the puddle didn't pick anything up");

            var bloodyTiles = 0;
            for (var x = 2; x < Width; x++)
            {
                var prints = GetFootprints(entMan, map.Grid, new Vector2i(x, 0));
                if (prints.Count == 0)
                    continue;

                bloodyTiles++;
                foreach (var print in prints)
                {
                    var solution = entMan.System<SharedSolutionContainerSystem>();
                    Assert.That(solution.TryGetSolution(print, "footprint", out _, out var printSolution));
                    Assert.That(printSolution!.GetTotalPrototypeQuantity(Blood), Is.GreaterThan(FixedPoint2.Zero));
                }
            }

            Assert.That(bloodyTiles, Is.GreaterThan(0), "No footprints were left after walking through blood");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// A puddle spilled over footprints absorbs them together with their reagents.
    /// </summary>
    [Test]
    public async Task PuddleAbsorbsFootprints()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();

        var mapSystem = entMan.System<SharedMapSystem>();
        var puddleSystem = entMan.System<PuddleSystem>();
        var tile = new Vector2i(3, 0);

        await server.WaitPost(() =>
        {
            PreparePlatform(entMan, map.Grid, roofed: true);
            var coords = mapSystem.GridTileToLocal(map.Grid, map.Grid, tile);
            SpawnFootprint(entMan, coords, 2);
            SpawnFootprint(entMan, coords, 2);

            puddleSystem.TrySpillAt(coords, new Solution(Water.Id, 5), out _, false);
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(GetFootprints(entMan, map.Grid, tile), Is.Empty);

            var puddle = GetPuddle(entMan, map.Grid, tile);
            Assert.That(puddle, Is.Not.Null);
            var solution = entMan.System<SharedSolutionContainerSystem>();
            Assert.That(solution.TryGetSolution(puddle!.Value.Owner, puddle.Value.Comp.SolutionName, out _, out var puddleSolution));
            Assert.That(puddleSolution!.GetTotalPrototypeQuantity(Blood), Is.EqualTo(FixedPoint2.New(4)));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Footprints evaporate under the open sky and stay under a roof.
    /// </summary>
    [Test]
    public async Task FootprintsEvaporateOutdoors()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();

        var mapSystem = entMan.System<SharedMapSystem>();
        EntityUid outdoor = default;
        EntityUid indoor = default;

        await server.WaitPost(() =>
        {
            PreparePlatform(entMan, map.Grid, roofed: false);
            entMan.System<SharedRoofSystem>().SetRoof((map.Grid, null), new Vector2i(5, 0), true);

            outdoor = SpawnFootprint(entMan, mapSystem.GridTileToLocal(map.Grid, map.Grid, new Vector2i(2, 0)), 0.05f);
            indoor = SpawnFootprint(entMan, mapSystem.GridTileToLocal(map.Grid, map.Grid, new Vector2i(5, 0)), 0.05f);
        });

        // One evaporation interval of the footprint plus a margin.
        await pair.RunSeconds(12);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(outdoor), "Footprint under the open sky didn't evaporate");
            Assert.That(entMan.Deleted(indoor), Is.False, "Footprint under a roof evaporated");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Walking back and forth over snow leaves temporary prints, capped per tile.
    /// </summary>
    [Test]
    public async Task WalkingOnSnowLeavesCappedTilePrints()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();

        var xformSystem = entMan.System<SharedTransformSystem>();
        EntityUid human = default;

        await server.WaitPost(() =>
        {
            PreparePlatform(entMan, map.Grid, roofed: true, Snow);
            human = entMan.SpawnEntity(Human, new EntityCoordinates(map.Grid, 0.5f, 0.5f));
        });

        await server.WaitPost(() =>
        {
            for (var pass = 0; pass < 6; pass++)
            {
                var forward = pass % 2 == 0;
                for (var i = 1; i <= (Width - 1) * 4; i++)
                {
                    var x = forward ? 0.5f + i * 0.25f : Width - 0.5f - i * 0.25f;
                    xformSystem.SetCoordinates(human, new EntityCoordinates(map.Grid, x, 0.5f));
                }
            }
        });

        await server.WaitAssertion(() =>
        {
            var mapSystem = entMan.System<SharedMapSystem>();
            var printedTiles = 0;
            for (var x = 0; x < Width; x++)
            {
                var prints = mapSystem.GetAnchoredEntities(map.Grid, map.Grid.Comp, new Vector2i(x, 0))
                    .Count(uid => entMan.HasComponent<CETileFootprintComponent>(uid));

                Assert.That(prints, Is.LessThanOrEqualTo(3), $"Tile {x} has more tile prints than the limit");
                if (prints > 0)
                    printedTiles++;
            }

            Assert.That(printedTiles, Is.GreaterThan(0), "Walking on snow left no prints");
        });

        await pair.CleanReturnAsync();
    }

    private const string Plating = "Plating";
    private const string Snow = "CESnow";

    private static void PreparePlatform(IEntityManager entMan, Entity<MapGridComponent> grid, bool roofed, string tileId = Plating)
    {
        var mapSystem = entMan.System<SharedMapSystem>();
        var tileMan = IoCManager.Resolve<ITileDefinitionManager>();
        var plating = new Tile(tileMan[tileId].TileId);

        // Gravity, so walking leaves prints instead of floating.
        var gravity = entMan.EnsureComponent<GravityComponent>(grid);
        entMan.System<GravitySystem>().EnableGravity(grid, gravity);

        entMan.EnsureComponent<RoofComponent>(grid);
        var roofSystem = entMan.System<SharedRoofSystem>();

        for (var x = 0; x < Width; x++)
        {
            mapSystem.SetTile(grid.Owner, grid.Comp, new Vector2i(x, 0), plating);
            roofSystem.SetRoof((grid, null), new Vector2i(x, 0), roofed);
        }
    }

    private static EntityUid SpawnFootprint(IEntityManager entMan, EntityCoordinates coords, float bloodVolume)
    {
        var print = entMan.SpawnEntity(Footprint, coords);
        var solution = entMan.System<SharedSolutionContainerSystem>();
        Assert.That(solution.TryGetSolution(print, "footprint", out var printSolution));
        solution.ForceAddSolution(printSolution!.Value, new Solution(Blood.Id, bloodVolume));
        return print;
    }

    private static List<EntityUid> GetFootprints(IEntityManager entMan, Entity<MapGridComponent> grid, Vector2i tile)
    {
        var mapSystem = entMan.System<SharedMapSystem>();
        return mapSystem.GetAnchoredEntities(grid, grid.Comp, tile)
            .Where(uid => entMan.HasComponent<CEFootprintComponent>(uid))
            .ToList();
    }

    private static Entity<PuddleComponent>? GetPuddle(IEntityManager entMan, Entity<MapGridComponent> grid, Vector2i tile)
    {
        var mapSystem = entMan.System<SharedMapSystem>();
        foreach (var uid in mapSystem.GetAnchoredEntities(grid, grid.Comp, tile))
        {
            if (entMan.TryGetComponent(uid, out PuddleComponent? puddle))
                return (uid, puddle);
        }

        return null;
    }

    private static FixedPoint2 GetPuddleVolume(IEntityManager entMan, Entity<MapGridComponent> grid, Vector2i tile)
    {
        var puddle = GetPuddle(entMan, grid, tile);
        if (puddle is null)
            return FixedPoint2.Zero;

        var solution = entMan.System<SharedSolutionContainerSystem>();
        return solution.TryGetSolution(puddle.Value.Owner, puddle.Value.Comp.SolutionName, out _, out var puddleSolution)
            ? puddleSolution.Volume
            : FixedPoint2.Zero;
    }
}
