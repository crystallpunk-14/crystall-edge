using System.Linq;

namespace Content.Shared._CE.Light;

public abstract class CESharedLightCycleSystem : EntitySystem
{
    /// <summary>
    /// Computes the ambient light color at the given point in the cycle by interpolating cyclically
    /// between the gradient stops in <see cref="CELightCycleComponent.Colors"/>.
    /// </summary>
    public static Color GetColor(CELightCycleComponent comp, TimeSpan duration, float time)
    {
        if (comp.Colors.Count == 0)
            return Color.White;

        if (comp.Colors.Count == 1)
            return comp.Colors.Values.First();

        var waveLength = MathF.Max(1f, (float) duration.TotalSeconds);
        var t = (time % waveLength) / waveLength;
        if (t < 0f)
            t += 1f;

        // Single allocation-free pass: find the stops around t, plus the first and last stops for the wrap segment
        // (last stop -> first stop + 1). Called on demand by gameplay light level queries, so no LINQ here.
        float? prevKey = null;
        float? nextKey = null;
        var prevColor = default(Color);
        var nextColor = default(Color);
        var minKey = float.MaxValue;
        var maxKey = float.MinValue;
        var minColor = default(Color);
        var maxColor = default(Color);

        foreach (var (key, color) in comp.Colors)
        {
            if (key < minKey)
            {
                minKey = key;
                minColor = color;
            }

            if (key > maxKey)
            {
                maxKey = key;
                maxColor = color;
            }

            if (key <= t && (prevKey is null || key > prevKey))
            {
                prevKey = key;
                prevColor = color;
            }

            if (key > t && (nextKey is null || key < nextKey))
            {
                nextKey = key;
                nextColor = color;
            }
        }

        if (prevKey is null)
        {
            prevKey = maxKey - 1f;
            prevColor = maxColor;
        }

        if (nextKey is null)
        {
            nextKey = minKey + 1f;
            nextColor = minColor;
        }

        var frac = (t - prevKey.Value) / (nextKey.Value - prevKey.Value);
        return Color.InterpolateBetween(prevColor, nextColor, frac);
    }
}
