using System.Numerics;
using Content.Shared._CE.Footprints;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Footprints;

/// <summary>
/// Blood splatters: blood taken from a body and thrown to the floor as a print, with an optional flying splash effect.
/// </summary>
public sealed partial class CEFootprintSystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    /// <summary>
    /// A wall between the body and the landing point shortens the throw down to this many tries.
    /// </summary>
    private const int SplatterShortenTries = 3;

    /// <summary>
    /// Takes up to <paramref name="volume"/> of blood from <paramref name="bleeder"/> and throws it
    /// <paramref name="distance"/> tiles in <paramref name="direction"/> as a print. A splatter landing on a puddle
    /// pours into it.
    /// </summary>
    /// <param name="rotation">World rotation of the print sprite.</param>
    /// <param name="vfx">Splash effect spawned <paramref name="vfxOffset"/> tiles out along the throw, facing it,
    /// and painted the blood's color through <see cref="SmokeVisuals.Color"/>.</param>
    /// <returns>False if the bleeder has no blood or there is no floor to land on.</returns>
    public bool TrySplatterBlood(EntityUid bleeder,
        Angle direction,
        float distance,
        FixedPoint2 volume,
        EntProtoId printProto,
        SpriteSpecifier sprite,
        Angle rotation,
        float maxAlpha,
        EntProtoId? vfx = null,
        float vfxOffset = 0f)
    {
        if (volume <= FixedPoint2.Zero || !_bloodstreamQuery.TryComp(bleeder, out var bloodstream))
            return false;

        if (!_solution.TryGetSolution(bleeder, bloodstream.BloodSolutionName, out var bloodEnt, out var blood) ||
            blood.Volume <= FixedPoint2.Zero)
            return false;

        var origin = _transform.GetMapCoordinates(bleeder);
        var throwVec = direction.ToWorldVec();
        var target = GetSplatterLanding(origin, throwVec, distance);

        if (!_map.TryFindGridAt(target, out var gridUid, out var grid))
            return false;

        var local = _transform.ToCoordinates(gridUid, target);
        var tile = _map.TileIndicesFor(gridUid, grid, local);
        if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        var taken = _solution.SplitSolution(bloodEnt.Value, volume);
        if (taken.Volume <= FixedPoint2.Zero)
            return false;

        // A drained body splatters paler.
        var color = taken.GetColor(_proto);
        color = color.WithAlpha(maxAlpha * Math.Clamp((taken.Volume / volume).Float(), 0f, 1f));

        if (vfx is { } vfxProto)
        {
            var splash = Spawn(vfxProto, origin.Offset(throwVec * vfxOffset));
            _transform.SetWorldRotation(splash, direction);
            _appearance.SetData(splash, SmokeVisuals.Color, color);
        }

        Entity<MapGridComponent> gridEnt = (gridUid, grid);
        if (TryGetPuddle(gridEnt, tile, out var puddle))
        {
            _puddle.TryAddSolution(puddle.Value, taken, sound: false);
            return true;
        }

        var gridRotation = _transform.GetWorldRotation(gridUid);
        if (!TrySpawnPrint(printProto, sprite, color, gridEnt, tile, local.Position, rotation - gridRotation, out var print, out var tileCenter))
            return true;

        if (!_solution.TryGetSolution(print.Value.Owner, print.Value.Comp.SolutionName, out var printEnt))
        {
            Log.Error($"{ToPrettyString(print)} is missing its {print.Value.Comp.SolutionName} solution");
            Del(print);
            return true;
        }

        _solution.ForceAddSolution(printEnt.Value, taken);
        TryMergeIntoPuddle(gridEnt, tile, tileCenter, print.Value.Comp.MaxTileVolume);
        return true;
    }

    /// <summary>
    /// Where a splatter thrown from <paramref name="origin"/> lands: shortened if a wall is in the way.
    /// </summary>
    private MapCoordinates GetSplatterLanding(MapCoordinates origin, Vector2 throwVec, float distance)
    {
        for (var i = 0; i < SplatterShortenTries && distance > 0f; i++)
        {
            var target = origin.Offset(throwVec * distance);
            if (_examine.InRangeUnOccluded(origin, target, distance + 0.1f, null))
                return target;

            distance *= 0.5f;
        }

        return origin;
    }
}
