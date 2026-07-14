using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._CE.Animation.Core;

/// <summary>
/// Debug overlay that draws ArcAttack hitboxes for 0.1 seconds when they fire.
/// Toggled via the "showarcattack" console command.
/// </summary>
public sealed partial class CEMeleeArcOverlay : Overlay
{
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly List<ArcAttackDebugEntry> _activeArcs = new();

    /// <summary>
    /// Duration in seconds to display each arc attack hitbox.
    /// </summary>
    private const float DisplayDuration = 0.1f;

    /// <summary>
    /// Number of segments used to approximate the arc curve.
    /// </summary>
    private const int ArcSegments = 24;

    public CEMeleeArcOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Adds a new arc attack to be displayed.
    /// </summary>
    public void AddArc(MapCoordinates position, Angle direction, float range, float arcWidth)
    {
        _activeArcs.Add(new ArcAttackDebugEntry
        {
            Position = position,
            Direction = direction,
            Range = range * 2f, // GetEntitiesInArc uses range * 2 for actual lookup
            ArcWidth = arcWidth,
            SpawnTime = _timing.CurTime,
        });
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var curTime = _timing.CurTime;

        // Remove expired arcs
        _activeArcs.RemoveAll(a => (curTime - a.SpawnTime).TotalSeconds > DisplayDuration);

        foreach (var arc in _activeArcs)
        {
            if (arc.Position.MapId != args.MapId)
                continue;

            DrawArc(args.WorldHandle, arc);
        }
    }

    private static void DrawArc(DrawingHandleWorld handle, ArcAttackDebugEntry arc)
    {
        var center = arc.Position.Position;
        var halfArc = arc.ArcWidth / 2.0;
        var directionDeg = arc.Direction.Degrees;

        var startAngle = Angle.FromDegrees(directionDeg - halfArc);
        var endAngle = Angle.FromDegrees(directionDeg + halfArc);

        var color = Color.Red.WithAlpha(0.35f);
        var outlineColor = Color.Red.WithAlpha(0.8f);

        // Draw filled arc as triangle fan
        var prevPoint = center + startAngle.RotateVec(new Vector2(arc.Range, 0));

        for (var i = 1; i <= ArcSegments; i++)
        {
            var t = (float) i / ArcSegments;
            var angle = Angle.FromDegrees(directionDeg - halfArc + arc.ArcWidth * t);
            var point = center + angle.RotateVec(new Vector2(arc.Range, 0));

            // Draw filled triangle
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,
                new[]
                {
                    center,
                    prevPoint,
                    point,
                }, color);

            prevPoint = point;
        }

        // Draw outline
        // Left edge
        var leftEnd = center + startAngle.RotateVec(new Vector2(arc.Range, 0));
        handle.DrawLine(center, leftEnd, outlineColor);

        // Right edge
        var rightEnd = center + endAngle.RotateVec(new Vector2(arc.Range, 0));
        handle.DrawLine(center, rightEnd, outlineColor);

        // Arc curve
        var arcPrev = leftEnd;
        for (var i = 1; i <= ArcSegments; i++)
        {
            var t = (float) i / ArcSegments;
            var angle = Angle.FromDegrees(directionDeg - halfArc + arc.ArcWidth * t);
            var point = center + angle.RotateVec(new Vector2(arc.Range, 0));
            handle.DrawLine(arcPrev, point, outlineColor);
            arcPrev = point;
        }
    }

    private sealed partial class ArcAttackDebugEntry
    {
        public MapCoordinates Position;
        public Angle Direction;
        public float Range;
        public float ArcWidth;
        public TimeSpan SpawnTime;
    }
}
