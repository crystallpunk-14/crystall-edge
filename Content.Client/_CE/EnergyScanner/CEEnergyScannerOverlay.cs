using System.Numerics;
using Content.Client._CE.Power.Components;
using Content.Client._CE.ZLevels.Core;
using Content.Client.Viewport;
using Content.Shared._CE.EnergyScanner;
using Content.Shared._CE.Power.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Wires;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._CE.EnergyScanner;

/// <summary>
/// Draws glowing lines over pipes on the viewer's z-level and one neighbouring level (below by default, above while
/// looking up). Everything is drawn in a single pass - the topmost rendered z-level - so tiles of upper levels never
/// cover the lines. Pipes of other levels are shifted by the same per-level offset the z-level renderer uses.
/// </summary>
public sealed partial class CEEnergyScannerOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedAppearanceSystem _appearance;
    private readonly SharedTransformSystem _transform;
    private readonly CEClientZLevelsSystem _zLevels;
    private readonly EntityQuery<CEEnergyLeakComponent> _leakQuery;

    private readonly HashSet<Entity<CEPipeVisualizerComponent>> _pipes = new();
    private readonly List<PipeDrawData> _largePipes = new();
    private readonly List<PipeDrawData> _mediumPipes = new();
    private readonly Vector2[] _segmentVertices = new Vector2[6];

    /// <summary>
    /// Depth (relative to the viewer) of the z-level being rendered when <see cref="Draw"/> runs.
    /// </summary>
    private int _passDepth;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CEEnergyScannerOverlay()
    {
        IoCManager.InjectDependencies(this);

        _lookup = _entMan.System<EntityLookupSystem>();
        _appearance = _entMan.System<SharedAppearanceSystem>();
        _transform = _entMan.System<SharedTransformSystem>();
        _zLevels = _entMan.System<CEClientZLevelsSystem>();
        _leakQuery = _entMan.GetEntityQuery<CEEnergyLeakComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return false;

        if (!_entMan.HasComponent<CEEnergyScannerViewerComponent>(player))
            return false;

        // Looking up: upper levels are rendered after the viewer's own level, so draw on the topmost one.
        if (args.Viewport.Eye is ScalingViewport.ZEye zEye)
        {
            if (zEye.Depth <= 0 || zEye.Depth != zEye.HighestDepth)
                return false;

            _passDepth = zEye.Depth;
            return true;
        }

        if (!_entMan.TryGetComponent<EyeComponent>(player, out var eye) || args.Viewport.Eye != eye.Eye)
            return false;

        if (_entMan.TryGetComponent<CEZLevelViewerComponent>(player, out var viewer) &&
            viewer.LookUp &&
            _zLevels.GetVisibleZLevelsAbove(player) > 0)
            return false;

        _passDepth = 0;
        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player ||
            !_entMan.TryGetComponent<CEEnergyScannerViewerComponent>(player, out var scanner) ||
            args.Viewport.Eye is not { } eye)
            return;

        var xform = _entMan.GetComponent<TransformComponent>(player);
        if (xform.MapUid is not { } mapUid)
            return;

        var lookUp = _entMan.TryGetComponent<CEZLevelViewerComponent>(player, out var viewer) && viewer.LookUp;

        // On-screen displacement from a z-level to the one above it, mirroring the z-level eye offsets.
        var levelStep = -(-eye.Rotation).ToWorldVec() * CESharedZLevelsSystem.ZLevelOffset;

        var handle = args.WorldHandle;

        // Neighbouring level first, so the viewer's own level is drawn on top.
        var neighborDepth = lookUp ? 1 : -1;
        if (_zLevels.TryMapOffset(mapUid, neighborDepth, out var neighborMap))
        {
            // Sine pulse between the min and peak alpha.
            var phase = (float) _timing.RealTime.TotalSeconds / scanner.NeighborLevelPulsePeriod * MathF.Tau;
            var pulse = 0.5f + 0.5f * MathF.Sin(phase);
            var neighborAlpha = MathHelper.Lerp(scanner.NeighborLevelMinAlpha, scanner.NeighborLevelAlpha, pulse);

            DrawLevel(handle,
                args.WorldAABB,
                _transform.GetMapId(neighborMap.Owner),
                neighborDepth,
                levelStep,
                scanner,
                neighborAlpha);
        }

        DrawLevel(handle, args.WorldAABB, xform.MapID, 0, levelStep, scanner, 1f);
    }

    private void DrawLevel(
        DrawingHandleWorld handle,
        Box2 worldAabb,
        MapId mapId,
        int depth,
        Vector2 levelStep,
        CEEnergyScannerViewerComponent style,
        float alpha)
    {
        // Where a pipe of this level ends up relative to its real position when drawn in the current pass.
        var shift = levelStep * (depth - _passDepth);

        _pipes.Clear();
        _lookup.GetEntitiesIntersecting(mapId, worldAabb.Translated(-shift).Enlarged(1f), _pipes);

        _largePipes.Clear();
        _mediumPipes.Clear();

        foreach (var pipe in _pipes)
        {
            var uid = pipe.Owner;

            _appearance.TryGetData<WireVisDirFlags>(uid, WireVisVisuals.ConnectedMask, out var mask);
            _appearance.TryGetData<bool>(uid, CEEnergyScannerVisuals.Powered, out var powered);
            _appearance.TryGetData<bool>(uid, CEEnergyScannerVisuals.Large, out var large);
            _appearance.TryGetData<CEPipeVerticalDirection>(uid, CEEnergyScannerVisuals.Vertical, out var vertical);
            var broken = _leakQuery.HasComp(uid);

            Color color;
            if (broken)
                color = style.BrokenColor;
            else if (!powered)
                color = style.UnpoweredColor;
            else
                color = large ? style.LargeColor : style.MediumColor;

            var data = new PipeDrawData(
                _transform.GetWorldPosition(uid) + shift + (large ? style.LargeOffset : style.MediumOffset),
                mask,
                vertical,
                large ? style.LargeWidth : style.MediumWidth,
                color.WithAlpha(color.A * alpha),
                powered || broken);

            if (large)
                _largePipes.Add(data);
            else
                _mediumPipes.Add(data);
        }

        // Big pipes on top of medium ones.
        DrawPipes(handle, _mediumPipes, levelStep, style, alpha);
        DrawPipes(handle, _largePipes, levelStep, style, alpha);
    }

    private void DrawPipes(
        DrawingHandleWorld handle,
        List<PipeDrawData> pipes,
        Vector2 levelStep,
        CEEnergyScannerViewerComponent style,
        float alpha)
    {
        foreach (var pipe in pipes)
        {
            if (!pipe.Glow)
                continue;

            var glowColor = pipe.Color.WithAlpha(style.GlowAlpha * alpha);
            DrawPipe(handle, pipe, levelStep, pipe.Width * style.GlowWidthMultiplier, glowColor);
        }

        foreach (var pipe in pipes)
        {
            DrawPipe(handle, pipe, levelStep, pipe.Width, pipe.Color);
        }
    }

    private void DrawPipe(DrawingHandleWorld handle, PipeDrawData pipe, Vector2 levelStep, float width, Color color)
    {
        var center = pipe.Position;
        var drawn = false;

        if ((pipe.Mask & WireVisDirFlags.North) != 0)
            drawn |= DrawSegment(handle, center, center + new Vector2(0f, 0.5f), width, color);
        if ((pipe.Mask & WireVisDirFlags.South) != 0)
            drawn |= DrawSegment(handle, center, center + new Vector2(0f, -0.5f), width, color);
        if ((pipe.Mask & WireVisDirFlags.East) != 0)
            drawn |= DrawSegment(handle, center, center + new Vector2(0.5f, 0f), width, color);
        if ((pipe.Mask & WireVisDirFlags.West) != 0)
            drawn |= DrawSegment(handle, center, center + new Vector2(-0.5f, 0f), width, color);

        // Half a level offset each way: the stubs of two connected levels meet in the middle.
        if ((pipe.Vertical & CEPipeVerticalDirection.Up) != 0)
            drawn |= DrawSegment(handle, center, center + levelStep * 0.5f, width, color);
        if ((pipe.Vertical & CEPipeVerticalDirection.Down) != 0)
            drawn |= DrawSegment(handle, center, center - levelStep * 0.5f, width, color);

        if (!drawn)
            handle.DrawRect(Box2.CenteredAround(center, new Vector2(width, width)), color);
    }

    /// <summary>
    /// Draws a thick line. The start is extended by half the width so segments meeting at a tile centre join cleanly.
    /// </summary>
    private bool DrawSegment(DrawingHandleWorld handle, Vector2 from, Vector2 to, float width, Color color)
    {
        var dir = to - from;
        var length = dir.Length();
        if (length <= 0f)
            return false;

        dir /= length;
        var half = width * 0.5f;
        var start = from - dir * half;
        var side = new Vector2(-dir.Y, dir.X) * half;

        _segmentVertices[0] = start + side;
        _segmentVertices[1] = start - side;
        _segmentVertices[2] = to - side;
        _segmentVertices[3] = start + side;
        _segmentVertices[4] = to - side;
        _segmentVertices[5] = to + side;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _segmentVertices, color);
        return true;
    }

    private readonly record struct PipeDrawData(
        Vector2 Position,
        WireVisDirFlags Mask,
        CEPipeVerticalDirection Vertical,
        float Width,
        Color Color,
        bool Glow);
}
