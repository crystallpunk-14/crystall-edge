using System.Numerics;

namespace Content.Shared._CE.Alchemy.World;

/// <summary>
/// Lays out the brewing map of a round: scatters zones in a spiral pattern, then runs a physical process.
/// Every iteration pushes circles apart (same-kind zones a bit harder, so useful and dangerous zones mix),
/// shoves ingredient points out of the way, and grows zones in proportion to their weights until the map is full.
/// A pure function of the seed and the inputs, so it is easy to check on many seeds.
/// </summary>
public static class CEAlchemyLayout
{
    /// <summary>
    /// Zones and ingredient points must fit inside this radius of the map.
    /// </summary>
    public const float MapRadius = 0.97f;

    /// <summary>
    /// Nothing but ingredient points may lie this close to the center: that is where every brew starts.
    /// </summary>
    public const float CenterClear = 0.12f;

    public const float CarrierMargin = 0.04f;
    public const float ZoneMargin = 0.03f;

    private const float CarrierRadius = 0.9f;
    private const float CarrierSpacing = 0.08f;
    private const float StartRadius = 0.03f;
    private const float MaxRadius = 0.2f;
    private const float GrowStep = 0.004f;
    private const float MixDistance = 0.1f;
    private const float MixPush = 0.05f;
    private const float CarrierShare = 0.7f;
    private const float SlackEpsilon = 1e-4f;
    private const int MaxIterations = 3000;
    private const int IdleIterations = 40;
    private const int RelaxPasses = 6;

    public sealed class Zone
    {
        public Vector2 Pos;
        public float Radius;
        public float Weight = 1f;
        public bool Dangerous;
        public float Growth = 1f;
    }

    public static void Generate(int seed, List<Zone> zones, List<Vector2> carriers)
    {
        var rand = new System.Random(seed);

        Scatter(rand, zones);

        var scale = StartRadius;
        var idle = 0;
        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            Relax(zones, carriers);

            scale += GrowStep;
            var grew = false;
            foreach (var zone in zones)
            {
                var target = MathF.Min(scale * zone.Weight * zone.Growth, MaxRadius * zone.Weight);
                if (zone.Radius >= target)
                    continue;

                var slack = Slack(zone, zones, false, carriers);
                if (slack <= SlackEpsilon)
                    continue;

                var radius = MathF.Min(target, zone.Radius + slack * 0.5f);
                if (radius - zone.Radius <= 1e-5f)
                    continue;

                zone.Radius = radius;
                grew = true;
            }

            idle = grew ? 0 : idle + 1;
            if (idle >= IdleIterations || scale > MaxRadius * 2)
                break;
        }

        Relax(zones, carriers);

        // Nothing may overlap: shrink what is still tight after the last push.
        for (var pass = 0; pass < 50; pass++)
        {
            var tight = false;
            foreach (var zone in zones)
            {
                var slack = Slack(zone, zones, true, carriers);
                if (slack >= -SlackEpsilon)
                    continue;

                zone.Radius = MathF.Max(0.01f, zone.Radius + slack - 1e-3f);
                tight = true;
            }

            if (!tight)
                break;
        }
    }

    /// <summary>
    /// Starting pattern: a golden-angle spiral around the clear center with a random turn, useful and dangerous zones alternate along it.
    /// </summary>
    private static void Scatter(System.Random rand, List<Zone> zones)
    {
        var useful = new List<Zone>();
        var dangerous = new List<Zone>();
        foreach (var zone in zones)
        {
            (zone.Dangerous ? dangerous : useful).Add(zone);
            zone.Radius = StartRadius * zone.Weight;
            zone.Growth = 0.8f + (float) rand.NextDouble() * 0.4f;
        }

        Shuffle(rand, useful);
        Shuffle(rand, dangerous);

        var order = new List<Zone>(zones.Count);
        var takeUseful = rand.Next(2) == 0;
        while (useful.Count > 0 || dangerous.Count > 0)
        {
            var list = (takeUseful && useful.Count > 0) || dangerous.Count == 0 ? useful : dangerous;
            order.Add(list[^1]);
            list.RemoveAt(list.Count - 1);
            takeUseful = !takeUseful;
        }

        var turn = rand.NextDouble() * Math.PI * 2;
        for (var k = 0; k < order.Count; k++)
        {
            var distance = 0.3 + 0.5 * Math.Sqrt((k + 0.5) / order.Count);
            var angle = turn + k * 2.399963;
            order[k].Pos = new Vector2((float) (distance * Math.Cos(angle)), (float) (distance * Math.Sin(angle)))
                           + new Vector2((float) (rand.NextDouble() * 0.1 - 0.05), (float) (rand.NextDouble() * 0.1 - 0.05));
        }
    }

    private static void Relax(List<Zone> zones, List<Vector2> carriers)
    {
        for (var pass = 0; pass < RelaxPasses; pass++)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                for (var j = i + 1; j < zones.Count; j++)
                    PushZones(zones[i], zones[j]);
            }

            foreach (var zone in zones)
            {
                for (var c = 0; c < carriers.Count; c++)
                {
                    var delta = carriers[c] - zone.Pos;
                    var distance = MathF.Max(delta.Length(), 1e-6f);
                    var need = zone.Radius + CarrierMargin;
                    if (distance >= need)
                        continue;

                    var push = delta / distance * (need - distance);
                    zone.Pos -= push * (1 - CarrierShare);
                    carriers[c] += push * CarrierShare;
                }

                var fromCenter = MathF.Max(zone.Pos.Length(), 1e-6f);
                if (fromCenter < zone.Radius + CenterClear)
                    zone.Pos *= (zone.Radius + CenterClear) / fromCenter;

                var limit = MapRadius - zone.Radius;
                var length = zone.Pos.Length();
                if (length > limit)
                    zone.Pos *= limit / length;
            }

            for (var a = 0; a < carriers.Count; a++)
            {
                for (var b = a + 1; b < carriers.Count; b++)
                {
                    var delta = carriers[b] - carriers[a];
                    var distance = MathF.Max(delta.Length(), 1e-6f);
                    if (distance >= CarrierSpacing)
                        continue;

                    var push = delta / distance * ((CarrierSpacing - distance) / 2);
                    carriers[a] -= push;
                    carriers[b] += push;
                }
            }

            for (var c = 0; c < carriers.Count; c++)
            {
                var length = carriers[c].Length();
                if (length > CarrierRadius)
                    carriers[c] *= CarrierRadius / length;
            }
        }
    }

    private static void PushZones(Zone a, Zone b)
    {
        var delta = b.Pos - a.Pos;
        var distance = MathF.Max(delta.Length(), 1e-6f);
        var need = a.Radius + b.Radius + ZoneMargin;

        // Zones of the same kind repel from further away: this is what mixes useful and dangerous ones.
        if (a.Dangerous == b.Dangerous && distance < need + MixDistance)
        {
            var soft = delta / distance * ((need + MixDistance - distance) * MixPush);
            a.Pos -= soft;
            b.Pos += soft;
            delta = b.Pos - a.Pos;
            distance = MathF.Max(delta.Length(), 1e-6f);
        }

        if (distance >= need)
            return;

        var push = delta / distance * ((need - distance) / 2);
        a.Pos -= push;
        b.Pos += push;
    }

    /// <summary>
    /// Free space around a zone: how much it could still grow before touching the edge, another zone or (optionally) a point.
    /// </summary>
    private static float Slack(Zone zone, List<Zone> zones, bool withCarriers, List<Vector2> carriers)
    {
        var slack = MathF.Min(MapRadius - zone.Pos.Length() - zone.Radius, zone.Pos.Length() - zone.Radius - CenterClear);
        foreach (var other in zones)
        {
            if (other == zone)
                continue;

            slack = MathF.Min(slack, Vector2.Distance(zone.Pos, other.Pos) - zone.Radius - other.Radius - ZoneMargin);
        }

        if (!withCarriers)
            return slack;

        foreach (var carrier in carriers)
        {
            slack = MathF.Min(slack, Vector2.Distance(zone.Pos, carrier) - zone.Radius - CarrierMargin);
        }

        return slack;
    }

    private static void Shuffle<T>(System.Random rand, List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rand.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
