using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client._CE.Actions;

/// <summary>
/// Draws spell targeting visuals: cast-radius circle, wide-line trajectory, cone, and AoE zone.
/// All visuals are drawn below entities but above the grid.
/// </summary>
public sealed partial class CEActionTargetingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;

    [Dependency] private IStateManager _stateManager = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SharedActionsSystem _actions;

    // Cached textures per RSI path+state — loaded lazily.
    private readonly Dictionary<(string path, string state), Texture> _textureCache = new();

    public CEActionTargetingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entManager.System<SharedTransformSystem>();
        _actions = _entManager.System<SharedActionsSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        if (_player.LocalEntity is not { } playerUid)
            return;

        var controller = _uiManager.GetUIController<ActionUIController>();
        if (controller.SelectingTargetFor is not { } actionUid)
            return;

        if (!_entManager.TryGetComponent<TransformComponent>(playerUid, out var playerXform))
            return;

        var playerMapPos = _transform.GetMapCoordinates(playerUid, xform: playerXform);
        if (playerMapPos.MapId != args.MapId)
            return;

        var mouseScreenPos = _input.MouseScreenPosition;
        var mouseMapPos = _eye.PixelToMap(mouseScreenPos);
        if (mouseMapPos.MapId != args.MapId)
            mouseMapPos = playerMapPos;

        var playerPos = playerMapPos.Position;
        var mousePos = mouseMapPos.Position;

        // Read TargetActionComponent for range.
        var range = 0f;
        if (_entManager.TryGetComponent<TargetActionComponent>(actionUid, out var targetComp))
            range = targetComp.Range;

        var hasEntityTarget = _entManager.HasComponent<EntityTargetActionComponent>(actionUid);
        var hasWorldTarget = _entManager.HasComponent<WorldTargetActionComponent>(actionUid);

        // 1) Cast-radius ring.
        if (_entManager.TryGetComponent<CEVisualizeRadiusTargetActionComponent>(actionUid, out var radiusVis))
        {
            DrawRing(handle,
                playerPos,
                range,
                radiusVis.Sprite.ToString(),
                radiusVis.State,
                radiusVis.SpriteSize,
                radiusVis.SpriteSpacing,
                Color.White,
                radiusVis.FillAlpha);
        }

        // 2) Wide-line trajectory.
        if (_entManager.TryGetComponent<CEVisualizeWideLineActionComponent>(actionUid, out var lineVis))
        {
            DrawWideLine(handle, playerUid, actionUid, playerPos, mousePos, range, hasEntityTarget, lineVis);
        }

        // 3) Cone.
        if (_entManager.TryGetComponent<CEVisualizeConeActionComponent>(actionUid, out var coneVis))
        {
            DrawCone(handle, playerUid, actionUid, playerPos, mousePos, range, hasEntityTarget, coneVis);
        }

        // 4) AoE zone.
        if (_entManager.TryGetComponent<CEVisualizeAoEZoneActionComponent>(actionUid, out var aoeVis))
        {
            DrawAoEZone(handle,
                playerUid,
                actionUid,
                playerPos,
                mousePos,
                range,
                hasEntityTarget,
                hasWorldTarget,
                aoeVis);
        }

        // 5) Tile highlight.
        if (_entManager.TryGetComponent<CEVisualizeTileActionComponent>(actionUid, out var tileVis))
        {
            DrawTile(handle, playerPos, mousePos, range, tileVis);
        }
    }

    #region Ring drawing

    /// <summary>
    /// Draws a ring of sprites around <paramref name="center"/> at <paramref name="radius"/>,
    /// each sprite facing inward toward center. A filled circle with low alpha is drawn inside.
    /// </summary>
    private void DrawRing(
        DrawingHandleWorld handle,
        Vector2 center,
        float radius,
        string spritePath,
        string state,
        float spriteSize,
        float spriteSpacing,
        Color color,
        float fillAlpha)
    {
        if (radius <= 0f)
            return;

        // Filled interior.
        handle.DrawCircle(center, radius, color.WithAlpha(fillAlpha));

        // Sprites around circumference.
        var texture = GetTexture(spritePath, state);
        if (texture == null)
        {
            // Fallback: just draw a thin circle outline.
            DrawCircleOutline(handle, center, radius, color.WithAlpha(0.6f), 48);
            return;
        }

        var circumference = MathF.Tau * radius;
        var spacing = spriteSpacing > 0f ? spriteSpacing : spriteSize;
        var count = Math.Max(8, (int)(circumference / spacing));
        var angleStep = MathF.Tau / count;

        for (var i = 0; i < count; i++)
        {
            var angle = angleStep * i;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var pos = center + dir * radius;

            // Sprite faces inward = rotation pointing from pos to center.
            var inwardAngle = MathF.Atan2(-dir.Y, -dir.X);
            var halfSize = spriteSize / 2f;
            var box = new Box2(-halfSize, -halfSize, halfSize, halfSize).Translated(pos);
            var rotated = new Box2Rotated(box, new Angle(inwardAngle), pos);

            handle.DrawTextureRect(texture, rotated, color);
        }
    }

    /// <summary>
    /// Draws a circle outline using line segments.
    /// </summary>
    private static void DrawCircleOutline(
        DrawingHandleWorld handle,
        Vector2 center,
        float radius,
        Color color,
        int segments)
    {
        var step = MathF.Tau / segments;
        var prev = center + new Vector2(radius, 0);

        for (var i = 1; i <= segments; i++)
        {
            var angle = step * i;
            var next = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            handle.DrawLine(prev, next, color);
            prev = next;
        }
    }

    #endregion

    #region Entity-snap helper

    /// <summary>
    /// Resolves the entity directly under the cursor using the exact same pixel-perfect
    /// sprite-picking that the engine uses for click input (<see cref="GameplayStateBase.GetClickedEntity"/>),
    /// then validates it through <see cref="SharedActionsSystem.ValidateEntityTarget"/> — the
    /// exact same codepath as the vanilla <c>OnEntityTargetAttempt</c>.
    /// Returns the entity's world position when it passes validation, <c>null</c> otherwise.
    /// </summary>
    private Vector2? FindSnapTarget(
        EntityUid playerUid,
        EntityUid actionUid,
        Vector2 mousePos)
    {
        // 1) Use the engine's sprite-picking to find the entity under the cursor — same as click.
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        var mapId = _eye.CurrentEye.Position.MapId;
        var mouseMapCoords = new MapCoordinates(mousePos, mapId);
        var entityUnderCursor = screen.GetClickedEntity(mouseMapCoords);

        if (entityUnderCursor is not { Valid: true } target)
            return null;

        // 2) Validate through the exact same method the ActionSystem uses.
        if (!_entManager.TryGetComponent<EntityTargetActionComponent>(actionUid, out var entityTargetComp))
            return null;

        if (!_actions.ValidateEntityTarget(playerUid, target, (actionUid, entityTargetComp)))
            return null;

        return _transform.GetWorldPosition(target);
    }

    #endregion

    #region Wide-line drawing

    private void DrawWideLine(
        DrawingHandleWorld handle,
        EntityUid playerUid,
        EntityUid actionUid,
        Vector2 playerPos,
        Vector2 mousePos,
        float range,
        bool hasEntityTarget,
        CEVisualizeWideLineActionComponent vis)
    {
        // Snap line end to a valid entity when in entity-target mode.
        Vector2 targetPos;
        if (hasEntityTarget)
        {
            targetPos = FindSnapTarget(playerUid, actionUid, mousePos) ?? mousePos;
        }
        else
        {
            targetPos = mousePos;
        }

        var direction = targetPos - playerPos;
        var distance = direction.Length();
        if (distance < 0.01f)
            return;

        var dirNorm = direction / distance;

        float lineLength;
        if (vis.ProjectileMode)
        {
            // In projectile mode, always draw full range in the cursor direction.
            lineLength = range > 0f ? range : distance;
        }
        else
        {
            lineLength = range > 0f ? MathF.Min(distance, range) : distance;
        }

        var endPos = playerPos + dirNorm * lineLength;

        // Perpendicular vector for width offset.
        var perp = new Vector2(-dirNorm.Y, dirNorm.X);
        var halfWidth = vis.Width / 2f;

        // Draw filled interior as a thin rectangle.
        {
            var bl = playerPos + perp * halfWidth;
            var br = playerPos - perp * halfWidth;
            var tl = endPos + perp * halfWidth;
            var tr = endPos - perp * halfWidth;
            var fillColor = Color.White.WithAlpha(vis.FillAlpha);

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,
                new[] { bl, br, tl, br, tr, tl },
                fillColor);
        }

        var angle = MathF.Atan2(dirNorm.Y, dirNorm.X);
        var rotation = new Angle(angle);

        // Draw border sprites — left side (start, stretched mid, end).
        DrawBorderStrip(handle,
            playerPos,
            endPos,
            lineLength,
            perp * halfWidth,
            rotation,
            vis.BorderStartSprite,
            vis.BorderStartState,
            vis.BorderMidSprite,
            vis.BorderMidState,
            vis.BorderEndSprite,
            vis.BorderEndState);

        // Draw border sprites — right side (mirrored, start, stretched mid, end).
        DrawBorderStrip(handle,
            playerPos,
            endPos,
            lineLength,
            perp * (-halfWidth),
            rotation,
            vis.BorderStartSprite,
            vis.BorderStartState,
            vis.BorderMidSprite,
            vis.BorderMidState,
            vis.BorderEndSprite,
            vis.BorderEndState);
    }

    private void DrawBorderStrip(
        DrawingHandleWorld handle,
        Vector2 start,
        Vector2 end,
        float length,
        Vector2 offset,
        Angle rotation,
        ResPath borderStartSprite,
        string borderStartState,
        ResPath borderMidSprite,
        string borderMidState,
        ResPath borderEndSprite,
        string borderEndState)
    {
        var capSize = 0.5f; // Size of start/end caps in world units.
        var halfCap = capSize / 2f;
        // Middle fills exactly the space between the inner edges of the two caps:
        //   start cap spans [-halfCap, +halfCap] around 'start+offset'
        //   end   cap spans [-halfCap, +halfCap] around 'end+offset'
        // -> middle span: [+halfCap, length-halfCap]  length = length-capSize
        var midLength = MathF.Max(0f, length - capSize);
        var dir = end - start;
        if (dir.Length() < 0.01f)
            return;

        var dirNorm = dir / dir.Length();

        var color = Color.White;
        var endCapRotation = rotation + Angle.FromDegrees(180);

        // Start cap — centred at start+offset.
        var startTex = GetTexture(borderStartSprite.ToString(), borderStartState);
        if (startTex != null)
        {
            var startPos = start + offset;
            var box = new Box2(-halfCap, -halfCap, halfCap, halfCap).Translated(startPos);
            handle.DrawTextureRect(startTex, new Box2Rotated(box, rotation, startPos), color);
        }

        // Stretched middle — starts at halfCap from 'start', ends at halfCap before 'end'.
        var midTex = GetTexture(borderMidSprite.ToString(), borderMidState);
        if (midTex != null && midLength > 0f)
        {
            var midCenter = start + offset + dirNorm * (halfCap + midLength / 2f);
            var halfMid = midLength / 2f;
            var box = new Box2(-halfMid, -halfCap, halfMid, halfCap).Translated(midCenter);
            handle.DrawTextureRect(midTex, new Box2Rotated(box, rotation, midCenter), color);
        }

        // End cap — centred at end+offset.
        var endTex = GetTexture(borderEndSprite.ToString(), borderEndState);
        if (endTex != null)
        {
            var endPos = end + offset;
            var box = new Box2(-halfCap, -halfCap, halfCap, halfCap).Translated(endPos);
            handle.DrawTextureRect(endTex, new Box2Rotated(box, endCapRotation, endPos), color);
        }
    }

    #endregion

    #region Cone drawing

    private void DrawCone(
        DrawingHandleWorld handle,
        EntityUid playerUid,
        EntityUid actionUid,
        Vector2 playerPos,
        Vector2 mousePos,
        float actionRange,
        bool hasEntityTarget,
        CEVisualizeConeActionComponent vis)
    {
        Vector2 targetPos;
        if (hasEntityTarget)
        {
            targetPos = FindSnapTarget(playerUid, actionUid, mousePos) ?? mousePos;
        }
        else
        {
            targetPos = mousePos;
        }

        var direction = targetPos - playerPos;
        var distance = direction.Length();
        if (distance < 0.01f || vis.Width <= 0.01f)
            return;

        var coneRange = vis.Range > 0f ? vis.Range : actionRange > 0f ? actionRange : distance;
        if (coneRange < 0.01f)
            return;

        var facing = new Angle(MathF.Atan2(direction.Y, direction.X));
        var halfWidth = vis.Width / 2f;
        var leftAngle = facing - Angle.FromDegrees(halfWidth);
        var rightAngle = facing + Angle.FromDegrees(halfWidth);

        var leftEnd = playerPos + leftAngle.RotateVec(new Vector2(coneRange, 0f));
        var rightEnd = playerPos + rightAngle.RotateVec(new Vector2(coneRange, 0f));

        DrawFilledCone(handle, playerPos, facing, coneRange, vis.Width, Color.White.WithAlpha(vis.FillAlpha));

        DrawBorderStrip(handle,
            playerPos,
            leftEnd,
            coneRange,
            Vector2.Zero,
            leftAngle,
            vis.BorderStartSprite,
            vis.BorderStartState,
            vis.BorderMidSprite,
            vis.BorderMidState,
            vis.BorderEndSprite,
            vis.BorderEndState);

        DrawBorderStrip(handle,
            playerPos,
            rightEnd,
            coneRange,
            Vector2.Zero,
            rightAngle,
            vis.BorderStartSprite,
            vis.BorderStartState,
            vis.BorderMidSprite,
            vis.BorderMidState,
            vis.BorderEndSprite,
            vis.BorderEndState);

        DrawConeArc(handle, playerPos, facing, coneRange, vis);
    }

    private static void DrawFilledCone(
        DrawingHandleWorld handle,
        Vector2 center,
        Angle facing,
        float range,
        float width,
        Color color)
    {
        var arcLength = MathF.Tau * range * (width / 360f);
        var segments = Math.Clamp((int) MathF.Ceiling(arcLength / 0.4f), 12, 64);
        var halfWidth = width / 2f;
        var startAngle = facing - Angle.FromDegrees(halfWidth);
        var prevPoint = center + startAngle.RotateVec(new Vector2(range, 0f));

        for (var i = 1; i <= segments; i++)
        {
            var t = (float) i / segments;
            var angle = startAngle + Angle.FromDegrees(width * t);
            var point = center + angle.RotateVec(new Vector2(range, 0f));

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,
                new[]
                {
                    center,
                    prevPoint,
                    point,
                }, color);

            prevPoint = point;
        }
    }

    private void DrawConeArc(
        DrawingHandleWorld handle,
        Vector2 center,
        Angle facing,
        float range,
        CEVisualizeConeActionComponent vis)
    {
        var halfWidth = vis.Width / 2f;
        var startAngle = facing - Angle.FromDegrees(halfWidth);
        var arcLength = MathF.Tau * range * (vis.Width / 360f);
        var segments = Math.Clamp((int) MathF.Ceiling(arcLength / 0.4f), 12, 64);

        var texture = GetTexture(vis.ArcSprite.ToString(), vis.ArcState);
        if (texture == null)
        {
            var prevPoint = center + startAngle.RotateVec(new Vector2(range, 0f));

            for (var i = 1; i <= segments; i++)
            {
                var t = (float) i / segments;
                var angle = startAngle + Angle.FromDegrees(vis.Width * t);
                var point = center + angle.RotateVec(new Vector2(range, 0f));
                handle.DrawLine(prevPoint, point, Color.White.WithAlpha(0.6f));
                prevPoint = point;
            }

            return;
        }

        var spacing = vis.ArcSpriteSpacing > 0f ? vis.ArcSpriteSpacing : vis.ArcSpriteSize;
        var count = Math.Max(1, (int) MathF.Ceiling(arcLength / MathF.Max(0.1f, spacing)));
        var halfSize = vis.ArcSpriteSize / 2f;

        for (var i = 0; i < count; i++)
        {
            var t = (i + 0.5f) / count;
            var angle = startAngle + Angle.FromDegrees(vis.Width * t);
            var pos = center + angle.RotateVec(new Vector2(range, 0f));

            var inwardAngle = new Angle(MathF.Atan2(center.Y - pos.Y, center.X - pos.X));
            var box = new Box2(-halfSize, -halfSize, halfSize, halfSize).Translated(pos);
            handle.DrawTextureRect(texture, new Box2Rotated(box, inwardAngle, pos), Color.White);
        }

    }

    #endregion

    #region AoE zone drawing

    private void DrawAoEZone(
        DrawingHandleWorld handle,
        EntityUid playerUid,
        EntityUid actionUid,
        Vector2 playerPos,
        Vector2 mousePos,
        float range,
        bool hasEntityTarget,
        bool hasWorldTarget,
        CEVisualizeAoEZoneActionComponent vis)
    {
        if (hasWorldTarget)
        {
            DrawAoEZoneWorld(handle, playerPos, mousePos, range, vis);
        }
        else if (hasEntityTarget)
        {
            DrawAoEZoneEntity(handle, playerUid, actionUid, playerPos, mousePos, range, vis);
        }
    }

    private void DrawAoEZoneWorld(
        DrawingHandleWorld handle,
        Vector2 playerPos,
        Vector2 mousePos,
        float range,
        CEVisualizeAoEZoneActionComponent vis)
    {
        var offset = mousePos - playerPos;
        var dist = offset.Length();

        Vector2 zoneCenter;
        bool inRange;

        if (vis.ProjectileMode)
        {
            // In projectile mode, the zone is always at max range in the cursor direction.
            if (dist < 0.01f)
            {
                zoneCenter = playerPos;
            }
            else
            {
                zoneCenter = playerPos + (offset / dist) * (range > 0f ? range : dist);
            }
            inRange = true;
        }
        else if (dist <= range || range <= 0f)
        {
            zoneCenter = mousePos;
            inRange = true;
        }
        else
        {
            // Clamp to range.
            zoneCenter = playerPos + (offset / dist) * range;
            inRange = false;
        }

        var color = inRange ? Color.White : Color.Red;

        DrawRing(handle,
            zoneCenter,
            vis.Radius,
            vis.Sprite.ToString(),
            vis.State,
            vis.SpriteSize,
            vis.SpriteSpacing,
            color,
            vis.FillAlpha);
    }

    private void DrawAoEZoneEntity(
        DrawingHandleWorld handle,
        EntityUid playerUid,
        EntityUid actionUid,
        Vector2 playerPos,
        Vector2 mousePos,
        float range,
        CEVisualizeAoEZoneActionComponent vis)
    {
        var snapPos = FindSnapTarget(playerUid, actionUid, mousePos);

        if (snapPos != null)
        {
            DrawRing(handle,
                snapPos.Value,
                vis.Radius,
                vis.Sprite.ToString(),
                vis.State,
                vis.SpriteSize,
                vis.SpriteSpacing,
                Color.White,
                vis.FillAlpha);
        }
        else
        {
            // No valid in-range entity — draw red zone clamped to cast boundary.
            var offset = mousePos - playerPos;
            var dist = offset.Length();
            var zoneCenter = dist <= range || range <= 0f
                ? mousePos
                : playerPos + (offset / dist) * range;

            DrawRing(handle,
                zoneCenter,
                vis.Radius,
                vis.Sprite.ToString(),
                vis.State,
                vis.SpriteSize,
                vis.SpriteSpacing,
                Color.Red,
                vis.FillAlpha);
        }
    }

    #endregion

    #region Tile highlight

    private static void DrawTile(
        DrawingHandleWorld handle,
        Vector2 playerPos,
        Vector2 mousePos,
        float range,
        CEVisualizeTileActionComponent vis)
    {
        var offset = mousePos - playerPos;
        var dist = offset.Length();

        Vector2 samplePos;
        bool inRange;

        if (range <= 0f || dist <= range)
        {
            // Cursor is within range — highlight tile under cursor.
            samplePos = mousePos;
            inRange = true;
        }
        else
        {
            // Cursor is outside range — clamp to the range boundary (same pattern as AoE zone),
            // so the highlight slides along the cast-radius edge instead of jumping to red at cursor.
            samplePos = playerPos + (offset / dist) * range;
            inRange = false;
        }

        // Snap sample point to the centre of its tile (tiles are 1×1 world units).
        var tileCenter = new Vector2(
            MathF.Floor(samplePos.X) + 0.5f,
            MathF.Floor(samplePos.Y) + 0.5f);

        var fillColor = (inRange ? Color.White : Color.Red).WithAlpha(vis.FillAlpha);
        var borderColor = (inRange ? Color.White : Color.Red).WithAlpha(vis.BorderAlpha);

        const float half = 0.5f;
        var box = new Box2(tileCenter.X - half, tileCenter.Y - half, tileCenter.X + half, tileCenter.Y + half);

        handle.DrawRect(box, fillColor);
        handle.DrawRect(box, borderColor, filled: false);
    }

    #endregion

    #region Texture helpers

    private Texture? GetTexture(string rsiPath, string state)
    {
        var key = (rsiPath, state);
        if (_textureCache.TryGetValue(key, out var cached))
            return cached;

        if (!_cache.TryGetResource<RSIResource>(new ResPath(rsiPath), out var rsi))
        {
            _textureCache[key] = null!;
            return null;
        }

        if (!rsi.RSI.TryGetState(state, out var rsiState))
        {
            _textureCache[key] = null!;
            return null;
        }

        var tex = rsiState.Frame0;
        _textureCache[key] = tex;
        return tex;
    }

    #endregion
}
