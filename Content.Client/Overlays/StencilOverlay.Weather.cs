using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class StencilOverlay
{
    private void DrawWeather(
        in OverlayDrawArgs args,
        HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>> weathers)
    {
        var worldHandle = args.WorldHandle;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;
        var eye = args.Viewport.Eye; //CrystallEdge: we need Eye for calculation of isometric wall offset direction

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        var stencil = _gridStencil.GetTileStencil(args,
            "weather-blocked",
            "weather-blocked-grid-stencil",
            (grid, tile) =>
            {
                if (eye is null) //CrystallEdge: isometric wall offset requires an eye to compute
                    return false;

                _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);
                // Ignored tiles for stencil.
                return !_weather.CanWeatherAffect((grid.Owner, grid.Comp, roofComp), tile);
            },
            ignoreEmpty: false, //CrystallEdge: we can have empty tiles under zLevel roof
            (grid, tile) => //CrystallEdge: isometric wall offset
            {
                Angle rotation = eye!.Rotation * -1f;
                var offset = rotation.ToWorldVec() * -0.5f;
                return new Box2(
                    tile.GridIndices * grid.Comp.TileSize + offset,
                    (tile.GridIndices + Vector2i.One) * grid.Comp.TileSize + offset);
            });

        var curTime = _timing.RealTime;

        // CrystallEdge: per-weather tiling offset derived from the weather effect's own EntityUid, shared by
        // the ground and drops layers, so stacked zNetwork levels don't render an identical pattern.
        var weatherOffsets = new Dictionary<EntityUid, Vector2>(weathers.Count);
        var hasGroundLayer = false;

        foreach (var (uid, weather, _) in weathers)
        {
            var hash = (uint)uid.GetHashCode();
            weatherOffsets[uid] = new Vector2((hash % 2000) - 1000f, ((hash / 2000) % 2000) - 1000f);

            if (weather.GroundSprite != null)
                hasGroundLayer = true;
        }

        // Ground/splash layer - drawn first so it sits underneath the falling-drops layer, and only on
        // solid (non-space) tiles that weather can actually affect, not over gaps/chasms.
        if (hasGroundLayer)
        {
            var groundStencil = _gridStencil.GetTileStencil(args,
                "weather-ground",
                "weather-ground-grid-stencil",
                (grid, tile) =>
                {
                    if (eye is null)
                        return false;

                    _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);
                    return !_weather.CanWeatherAffect((grid.Owner, grid.Comp, roofComp), tile) || _turf.IsSpace(tile);
                },
                ignoreEmpty: false,
                (grid, tile) =>
                {
                    Angle rotation = eye!.Rotation * -1f;
                    var offset = rotation.ToWorldVec() * -0.5f;
                    return new Box2(
                        tile.GridIndices * grid.Comp.TileSize + offset,
                        (tile.GridIndices + Vector2i.One) * grid.Comp.TileSize + offset);
                });

            worldHandle.SetTransform(Matrix3x2.Identity);
            worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
            worldHandle.DrawTextureRect(groundStencil.Texture, worldBounds);

            foreach (var (uid, weather, status) in weathers)
            {
                if (weather.GroundSprite is not { } groundSpriteSpecifier)
                    continue;

                var alpha = _weather.GetWeatherPercent((uid, status));
                var groundSprite = _sprite.GetFrame(groundSpriteSpecifier, curTime);
                var weatherOffset = weatherOffsets[uid];

                worldHandle.UseShader(_protoManager.Index(StencilDraw).Instance());
                worldHandle.SetTransform(Matrix3x2.CreateTranslation(-weatherOffset));
                _parallax.DrawParallax(worldHandle,
                    new Box2(worldAABB.BottomLeft + weatherOffset, worldAABB.TopRight + weatherOffset),
                    groundSprite,
                    curTime,
                    position,
                    Vector2.Zero,
                    modulate: (weather.Color ?? Color.White).WithAlpha(alpha));
                worldHandle.SetTransform(Matrix3x2.Identity);
            }

            // The ground mask above left stencil bits set for tiles it blocked (including space tiles the
            // drops layer below is allowed to cover) - clear them so the drops layer's own mask starts clean.
            worldHandle.UseShader(_protoManager.Index(StencilClear).Instance());
            worldHandle.DrawTextureRect(groundStencil.Texture, worldBounds);
        }
        // CrystallEdge end

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(stencil.Texture, worldBounds);

        foreach (var (uid, weather, status) in weathers)
        {
            var alpha = _weather.GetWeatherPercent((uid, status));
            var sprite = _sprite.GetFrame(weather.Sprite, curTime);
            var weatherOffset = weatherOffsets[uid]; //CrystallEdge: per-map weather tiling offset

            // Draw the rain
            worldHandle.UseShader(_protoManager.Index(StencilDraw).Instance());
            worldHandle.SetTransform(Matrix3x2.CreateTranslation(-weatherOffset)); //CrystallEdge: compensate so the shifted AABB below still renders at the correct screen position
            _parallax.DrawParallax(worldHandle,
                new Box2(worldAABB.BottomLeft + weatherOffset, worldAABB.TopRight + weatherOffset), //CrystallEdge: offset AABB to shift tiling phase per map
                sprite,
                curTime,
                position,
                weather.Scrolling ?? Vector2.Zero,
                modulate: (weather.Color ?? Color.White).WithAlpha(alpha));
            worldHandle.SetTransform(Matrix3x2.Identity); //CrystallEdge: reset transform after the per-map offset compensation
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }
}
