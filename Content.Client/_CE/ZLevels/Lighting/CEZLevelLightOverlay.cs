using System.Numerics;
using Content.Client.Light;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.ZLevels.Lighting;

public sealed partial class CEZLevelLightOverlay : Overlay
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    private readonly CESharedZLevelsSystem _zLevels;
    private readonly LightTreeSystem _lightTree;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _xform;

    private readonly EntityQuery<CEZMapComponent> _zMapQuery;
    private readonly EntityQuery<MapComponent> _mapQuery;
    private readonly EntityQuery<OccluderComponent> _occluderQuery;

    private static readonly ProtoId<ShaderPrototype> AdditiveShader = "CEZLightAdditive";

    private readonly ShaderInstance _additive;

    /// <summary>
    /// Minimum contribution that can affect the 8bit lighting buffer
    /// </summary>
    private const float MinContribution = 1f / 255f;

    /// <summary>
    /// Accumulated light per world tile shared across grids and z-levels
    /// </summary>
    private readonly Dictionary<Vector2i, Vector3> _accumulator = new();

    /// <summary>
    /// Caches occluder checks for the current level to avoid repeated line of sight walks
    /// </summary>
    private readonly Dictionary<Vector2i, bool> _occluderCache = new();

    private readonly List<Entity<PointLightComponent, TransformComponent>> _lights = new();
    private List<Entity<MapGridComponent>> _grids = new();

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public CEZLevelLightOverlay(IEntityManager entity)
    {
        IoCManager.InjectDependencies(this);

        _zLevels = entity.System<CESharedZLevelsSystem>();
        _lightTree = entity.System<LightTreeSystem>();
        _map = entity.System<SharedMapSystem>();
        _xform = entity.System<SharedTransformSystem>();

        _zMapQuery = entity.GetEntityQuery<CEZMapComponent>();
        _mapQuery = entity.GetEntityQuery<MapComponent>();
        _occluderQuery = entity.GetEntityQuery<OccluderComponent>();

        _additive = _proto.Index(AdditiveShader).Instance();

        ZIndex = TileEmissionOverlay.ContentZIndex; // Runs alongside TileEmissionOverlay before LightBlurOverlay
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace || args.Viewport.Eye is not { DrawLight: true })
            return false;

        if (!_cfg.GetCVar(CCVars.CEZLevelsLightEnabled))
            return false;

        return _zMapQuery.HasComp(args.MapUid) && _mapQuery.HasComp(args.MapUid);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        if (!_zMapQuery.TryComp(args.MapUid, out var zMap))
            return;

        var viewport = args.Viewport;
        var before = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var target = before.GetCachedForViewport(viewport).EnlargedLightTarget;

        var viewAabb = before.EnlargedBounds.CalcBoundingBox();

        _accumulator.Clear();

        var maxLevels = Math.Max(0, _cfg.GetCVar(CCVars.CEZLevelsLightMaxLevels));
        var perLevel = Math.Clamp(_cfg.GetCVar(CCVars.CEZLevelsLightTransmission), 0f, 1f);
        var maxLights = Math.Max(0, _cfg.GetCVar(CCVars.CEZLevelsLightMaxLights));
        var occlusion = _cfg.GetCVar(CCVars.CEZLevelsLightOcclusion);

        var current = new Entity<CEZMapComponent?>(args.MapUid, zMap);
        var transmission = 1f;

        for (var level = 1; level <= maxLevels; level++)
        {
            transmission *= perLevel;
            if (transmission < MinContribution)
                break;

            if (!_zLevels.TryMapOffset(current, 1, out var above, out var aboveMap))
                break;

            AccumulateLevel(aboveMap.MapId, viewAabb, transmission, maxLights, occlusion);
            current = above.AsNullable();
        }

        if (_accumulator.Count == 0)
            return;

        // same light target scaling dance as the upstream light overlays
        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var worldHandle = args.WorldHandle;

        worldHandle.RenderInRenderTarget(target,
            () =>
            {
                worldHandle.SetTransform(target.GetWorldToLocalMatrix(eye, scale));
                worldHandle.UseShader(_additive);

                foreach (var (tile, value) in _accumulator)
                {
                    var color = new Color(
                        Math.Clamp(value.X, 0f, 1f),
                        Math.Clamp(value.Y, 0f, 1f),
                        Math.Clamp(value.Z, 0f, 1f));

                    worldHandle.DrawRect(new Box2(tile.X, tile.Y, tile.X + 1, tile.Y + 1), color);
                }

                worldHandle.UseShader(null);
            },
            null);

        worldHandle.SetTransform(Matrix3x2.Identity);
    }

    private void AccumulateLevel(MapId aboveMapId, Box2 viewAabb, float transmission, int maxLights, bool occlusion)
    {
        _lights.Clear();
        _lightTree.QueryAabb(_lights, aboveMapId, viewAabb);

        if (_lights.Count == 0)
            return;

        _occluderCache.Clear();

        var used = 0;

        foreach (var (_, light, xform) in _lights)
        {
            if (used >= maxLights)
                break;

            if (!light.Enabled || light.ContainerOccluded || light.Radius <= 0f || light.Energy <= 0f)
                continue;

            var (worldPos, worldRot) = _xform.GetWorldPositionRotation(xform);
            var lightPos = worldPos + worldRot.RotateVec(light.Offset);
            var radius = light.Radius;

            var minX = MathF.Max(lightPos.X - radius, viewAabb.Left);
            var maxX = MathF.Min(lightPos.X + radius, viewAabb.Right);
            var minY = MathF.Max(lightPos.Y - radius, viewAabb.Bottom);
            var maxY = MathF.Min(lightPos.Y + radius, viewAabb.Top);

            if (minX > maxX || minY > maxY)
                continue;

            used++;

            _grids.Clear();
            _map.FindGridsIntersecting(
                aboveMapId,
                new Box2(lightPos.X - radius, lightPos.Y - radius, lightPos.X + radius, lightPos.Y + radius),
                ref _grids,
                approx: true,
                includeMap: true);

            var lightTile = new Vector2i((int) MathF.Floor(lightPos.X), (int) MathF.Floor(lightPos.Y));
            var radiusSquared = radius * radius;
            var color = light.Color;

            var tileMinX = (int) MathF.Floor(minX);
            var tileMaxX = (int) MathF.Floor(maxX);
            var tileMinY = (int) MathF.Floor(minY);
            var tileMaxY = (int) MathF.Floor(maxY);

            for (var x = tileMinX; x <= tileMaxX; x++)
            {
                for (var y = tileMinY; y <= tileMaxY; y++)
                {
                    var center = new Vector2(x + 0.5f, y + 0.5f);
                    var distanceSquared = Vector2.DistanceSquared(lightPos, center);
                    if (distanceSquared > radiusSquared)
                        continue;

                    var value = Attenuate(
                        MathF.Sqrt(distanceSquared),
                        radius,
                        light.Energy,
                        light.Falloff,
                        light.CurveFactor) * transmission;

                    if (value < MinContribution)
                        continue;

                    if (IsCeilingSolid(center))
                        continue;

                    var tile = new Vector2i(x, y);

                    if (occlusion && !HasLineOfSight(lightTile, tile))
                        continue;

                    _accumulator.TryGetValue(tile, out var accumulated);
                    _accumulator[tile] = accumulated + new Vector3(color.R, color.G, color.B) * value;
                }
            }
        }
    }

    /// <summary>
    /// Engine point light attenuation matching the original light level
    /// </summary>
    private static float Attenuate(float distance, float radius, float energy, float falloff, float curveFactor)
    {
        // LIGHTING_HEIGHT in the shader
        var squaredDistance = distance * distance + 1f;

        var s = Math.Clamp(MathF.Sqrt(squaredDistance) / radius, 0f, 1f);
        var s2 = s * s;
        var curve = MathHelper.Lerp(s, s2, Math.Clamp(curveFactor, 0f, 1f));
        var value = Math.Clamp((1f - s2) * (1f - s2) / (1f + falloff * curve), 0f, 1f);

        return value * energy;
    }

    private bool IsCeilingSolid(Vector2 worldPos)
    {
        foreach (var grid in _grids)
        {
            if (!_map.TryGetTileRef(grid.Owner, grid.Comp, worldPos, out var tileRef))
                continue;

            if (!CEZLevelOpeningCache.IsOpeningTile(tileRef.Tile, _tileDef))
                return true;
        }

        return false;
    }

    private bool HasLineOfSight(Vector2i from, Vector2i to)
    {
        if (from == to)
            return true;

        var dx = Math.Abs(to.X - from.X);
        var dy = Math.Abs(to.Y - from.Y);
        var stepX = from.X < to.X ? 1 : -1;
        var stepY = from.Y < to.Y ? 1 : -1;
        var error = dx - dy;
        var x = from.X;
        var y = from.Y;

        while (true)
        {
            var doubledError = error * 2;

            if (doubledError > -dy)
            {
                error -= dy;
                x += stepX;
            }

            if (doubledError < dx)
            {
                error += dx;
                y += stepY;
            }

            if (x == to.X && y == to.Y)
                return true;

            if (TileHasOccluder(new Vector2i(x, y)))
                return false;
        }
    }

    private bool TileHasOccluder(Vector2i worldTile)
    {
        if (_occluderCache.TryGetValue(worldTile, out var cached))
            return cached;

        var worldPos = new Vector2(worldTile.X + 0.5f, worldTile.Y + 0.5f);
        var occluded = false;

        foreach (var grid in _grids)
        {
            var indices = _map.WorldToTile(grid.Owner, grid.Comp, worldPos);
            var anchored = _map.GetAnchoredEntitiesEnumerator(grid.Owner, grid.Comp, indices);

            while (anchored.MoveNext(out var uid))
            {
                if (!_occluderQuery.TryComp(uid.Value, out var occluder) || !occluder.Enabled)
                    continue;

                occluded = true;
                break;
            }

            if (occluded)
                break;
        }

        _occluderCache[worldTile] = occluded;
        return occluded;
    }
}
