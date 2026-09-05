using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Achievements.UI;

/// <summary>
/// A simple control that draws a horizontal fill bar based on a percentage value.
/// Used as a background layer inside achievement entries to show popularity.
/// </summary>
public sealed class CEAchievementFillBar : Control
{
    /// <summary>
    /// Fill percentage (0–100).
    /// </summary>
    public float FillPercent { get; set; }

    /// <summary>
    /// Color of the filled portion.
    /// </summary>
    public Color FillColor { get; set; } = new(0.25f, 0.50f, 0.25f, 0.5f);

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var fillWidth = size.X * Math.Clamp(FillPercent / 100f, 0f, 1f);

        if (fillWidth <= 0f)
            return;

        var rect = UIBox2.FromDimensions(Vector2.Zero, new Vector2(fillWidth, size.Y));
        handle.DrawRect(rect, FillColor);
    }
}
