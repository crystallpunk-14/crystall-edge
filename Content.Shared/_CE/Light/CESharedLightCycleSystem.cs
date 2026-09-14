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

        var stops = comp.Colors.OrderBy(kv => kv.Key).ToList();

        // Values before the first stop belong to the tail of the wrap segment (previous stop -> first stop + 1).
        if (t < stops[0].Key)
            t += 1f;

        for (var i = 0; i < stops.Count - 1; i++)
        {
            if (t < stops[i].Key || t >= stops[i + 1].Key)
                continue;

            var frac = (t - stops[i].Key) / (stops[i + 1].Key - stops[i].Key);
            return Color.InterpolateBetween(stops[i].Value, stops[i + 1].Value, frac);
        }

        var last = stops[^1];
        var wrapKey = stops[0].Key + 1f;
        var wrapFrac = (t - last.Key) / (wrapKey - last.Key);
        return Color.InterpolateBetween(last.Value, stops[0].Value, wrapFrac);
    }
}
