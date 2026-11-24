using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._CE.Guidebook.Controls;

/// <summary>
/// Draws the brewing map of the alchemy sandbox: zones, carriers, the marker path and the marker itself.
/// Map coordinates run from -1 to 1 in both axes, the Y axis points down like on screen.
/// </summary>
public sealed partial class CEAlchemyMapControl : Control
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly Color Background = Color.FromHex("#14181b");
    private static readonly Color Rim = Color.FromHex("#3c464c");
    private static readonly Color Grid = Color.FromHex("#252d32");
    private static readonly Color PositionZone = Color.FromHex("#4fb59a");
    private static readonly Color PathZone = Color.FromHex("#e08a4a");
    private static readonly Color PathLine = Color.FromHex("#e3e1da");
    private static readonly Color MarkerColor = Color.FromHex("#ffd86b");
    private static readonly Color TooltipBack = Color.FromHex("#0c0f11");

    /// <summary>
    /// Map corner angles in degrees: the six anonymous sides of the brewing map.
    /// </summary>
    private static readonly float[] CornerAngles = [-90, -30, 30, 90, 150, 210];

    private readonly Font _font;

    private readonly List<MapDot> _dots = new();
    private readonly List<MapZone> _zones = new();
    private readonly List<Vector2> _trail = new();
    private Vector2? _marker;
    private (Vector2 From, Vector2 To)? _preview;

    public CEAlchemyMapControl()
    {
        IoCManager.InjectDependencies(this);

        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 11);

        MinSize = new Vector2(340, 340);
        RectClipContent = true;
    }

    public void SetStatic(IEnumerable<MapDot> dots, IEnumerable<MapZone> zones)
    {
        _dots.Clear();
        _dots.AddRange(dots);
        _zones.Clear();
        _zones.AddRange(zones);
    }

    public void SetPath(IReadOnlyList<Vector2> trail, Vector2? marker)
    {
        _trail.Clear();
        _trail.AddRange(trail);
        _marker = marker;
    }

    /// <summary>
    /// Shows where the marker would go if the selected pour was added. Null hides the preview.
    /// </summary>
    public void SetPreview((Vector2 From, Vector2 To)? preview)
    {
        _preview = preview;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var center = PixelSize / 2;
        var radius = MathF.Min(PixelSize.X, PixelSize.Y) / 2 - 8 * UIScale;
        if (radius <= 0)
            return;

        var cursor = UserInterfaceManager.MousePositionScaled.Position * UIScale - GlobalPixelPosition;

        // Board.
        handle.DrawCircle(center, radius, Background);
        handle.DrawCircle(center, radius * 0.5f, Grid, false);
        handle.DrawLine(center - new Vector2(radius, 0), center + new Vector2(radius, 0), Grid);
        handle.DrawLine(center - new Vector2(0, radius), center + new Vector2(0, radius), Grid);

        // The hexagon with the six sides of the map, drawn as soft spokes.
        for (var i = 0; i < CornerAngles.Length; i++)
        {
            var a = ToPixel(Corner(CornerAngles[i]), center, radius);
            var b = ToPixel(Corner(CornerAngles[(i + 1) % CornerAngles.Length]), center, radius);
            handle.DrawLine(center, a, Grid);
            handle.DrawLine(a, b, Rim);
        }

        handle.DrawCircle(center, radius, Rim, false);

        // Zones.
        var hover = string.Empty;
        foreach (var zone in _zones)
        {
            var color = zone.ByPath ? PathZone : PositionZone;
            var pos = ToPixel(zone.Pos, center, radius);
            var r = zone.Radius * radius;
            handle.DrawCircle(pos, r, color.WithAlpha(0.16f));
            handle.DrawCircle(pos, r * 0.55f, color.WithAlpha(0.14f));
            handle.DrawCircle(pos, r, color.WithAlpha(0.85f), false);

            if ((cursor - pos).LengthSquared() <= r * r)
                hover = zone.Label;
        }

        // Path of the marker.
        for (var i = 0; i + 1 < _trail.Count; i++)
        {
            var from = ToPixel(_trail[i], center, radius);
            var to = ToPixel(_trail[i + 1], center, radius);
            DrawThickLine(handle, from, to, 2.5f * UIScale, PathLine.WithAlpha(0.8f));
        }

        for (var i = 1; i + 1 < _trail.Count; i++)
        {
            handle.DrawCircle(ToPixel(_trail[i], center, radius), 3.5f * UIScale, PathLine);
        }

        // Preview of the pour that is about to be added.
        if (_preview is { } preview)
        {
            var from = ToPixel(preview.From, center, radius);
            var to = ToPixel(preview.To, center, radius);
            DrawThickLine(handle, from, to, 2.5f * UIScale, MarkerColor.WithAlpha(0.55f));
            handle.DrawCircle(to, 7f * UIScale, MarkerColor.WithAlpha(0.18f));
            handle.DrawCircle(to, 7f * UIScale, MarkerColor.WithAlpha(0.9f), false);
        }

        // Carriers.
        var hoverDot = float.MaxValue;
        foreach (var dot in _dots)
        {
            var pos = ToPixel(dot.Pos, center, radius);
            handle.DrawCircle(pos, 6f * UIScale, Background);
            handle.DrawCircle(pos, 4.5f * UIScale, dot.Color);
            handle.DrawCircle(pos, 6f * UIScale, Color.White.WithAlpha(0.7f), false);

            var distance = (cursor - pos).Length();
            if (distance <= 9f * UIScale && distance < hoverDot)
            {
                hoverDot = distance;
                hover = dot.Label;
            }
        }

        // Marker with a pulsing halo.
        if (_marker is { } marker)
        {
            var pos = ToPixel(marker, center, radius);
            var pulse = 0.5f + 0.5f * MathF.Sin((float) _timing.RealTime.TotalSeconds * 3f);
            handle.DrawCircle(pos, (11f + 4f * pulse) * UIScale, MarkerColor.WithAlpha(0.12f + 0.1f * pulse));
            handle.DrawCircle(pos, 7.5f * UIScale, MarkerColor.WithAlpha(0.35f));
            handle.DrawCircle(pos, 5f * UIScale, MarkerColor);
            handle.DrawCircle(pos, 5f * UIScale, Color.Black.WithAlpha(0.6f), false);
        }

        if (hover != string.Empty)
            DrawTooltip(handle, cursor, hover);
    }

    private void DrawTooltip(DrawingHandleScreen handle, Vector2 cursor, string text)
    {
        var size = new Vector2(0, 0);
        foreach (var rune in text.EnumerateRunes())
        {
            if (_font.TryGetCharMetrics(rune, UIScale, out var metrics))
                size.X += metrics.Advance;
        }

        size.Y = _font.GetHeight(UIScale);

        var pad = 5f * UIScale;
        var topLeft = cursor + new Vector2(14, 14) * UIScale;
        topLeft.X = MathF.Min(topLeft.X, PixelSize.X - size.X - pad * 2);
        topLeft.Y = MathF.Min(topLeft.Y, PixelSize.Y - size.Y - pad * 2);
        topLeft = Vector2.Max(topLeft, Vector2.Zero);

        var box = new UIBox2(topLeft, topLeft + size + new Vector2(pad * 2, pad * 2));
        handle.DrawRect(box, TooltipBack.WithAlpha(0.92f));
        handle.DrawRect(box, Rim, false);
        handle.DrawString(_font, topLeft + new Vector2(pad, pad), text, Color.FromHex("#e3e1da"));
    }

    private static void DrawThickLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, float width, Color color)
    {
        var dir = to - from;
        if (dir.LengthSquared() < 0.0001f)
            return;

        var normal = Vector2.Normalize(new Vector2(-dir.Y, dir.X)) * (width / 2);
        var quad = new[]
        {
            from - normal, from + normal, to + normal,
            from - normal, to + normal, to - normal,
        };
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, quad, color);
    }

    private static Vector2 Corner(float degrees)
    {
        var rad = MathHelper.DegreesToRadians(degrees);
        return new Vector2(MathF.Cos(rad), MathF.Sin(rad));
    }

    private static Vector2 ToPixel(Vector2 map, Vector2 center, float radius)
    {
        return center + map * radius;
    }

    /// <summary>
    /// A carrier reagent on the map.
    /// </summary>
    public readonly record struct MapDot(string Label, Vector2 Pos, Color Color);

    /// <summary>
    /// A zone circle on the map.
    /// </summary>
    public readonly record struct MapZone(string Label, Vector2 Pos, float Radius, bool ByPath);
}
