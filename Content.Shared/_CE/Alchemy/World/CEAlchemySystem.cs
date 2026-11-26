using System.Linq;
using System.Numerics;
using Content.Shared._CE.Alchemy.Data;
using Content.Shared._CE.Alchemy.Effects;
using Content.Shared._CE.Alchemy.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Alchemy.World;

/// <summary>
/// Alchemy logic shared by server and client: the brewing map of the round, recipe replay and analysis.
/// </summary>
public sealed partial class CEAlchemySystem : EntitySystem
{
    /// <summary>
    /// Share of the zone radius taken by its core: a path through the core gives the full effect.
    /// </summary>
    public const float CoreRatio = 0.25f;

    /// <summary>
    /// Effect share in the outer ring next to the core.
    /// </summary>
    private const float RingInnerShare = 0.5f;

    /// <summary>
    /// Effect share in the outer ring at the edge of the zone.
    /// </summary>
    private const float RingOuterShare = 0.2f;

    [Dependency] private IPrototypeManager _proto = default!;

    /// <summary>
    /// Finds the single round world entity. False before it exists (lobby, client before the state arrives).
    /// </summary>
    public bool TryGetWorld(out Entity<CEAlchemyWorldComponent> world)
    {
        var query = EntityQueryEnumerator<CEAlchemyWorldComponent>();
        if (query.MoveNext(out var uid, out var comp))
        {
            world = (uid, comp);
            return true;
        }

        world = default;
        return false;
    }

    /// <summary>
    /// Layout used when there is no round world (lobby guidebook): the same layout as a round with seed 0.
    /// It is a cache derived from prototypes and is dropped when they reload.
    /// </summary>
    private CEAlchemyWorldComponent? _fallback;

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _fallback = null;
    }

    private CEAlchemyWorldComponent GetLayout()
    {
        if (TryGetWorld(out var world))
            return world.Comp;

        if (_fallback == null)
        {
            _fallback = new CEAlchemyWorldComponent();
            FillWorld(_fallback, 0);
        }

        return _fallback;
    }

    /// <summary>
    /// Lays out carriers and zones of the map for the seed (see <see cref="CEAlchemyLayout"/>).
    /// </summary>
    public void FillWorld(CEAlchemyWorldComponent comp, int seed)
    {
        comp.Seed = seed;
        comp.Carriers.Clear();
        comp.Zones.Clear();
        comp.Radii.Clear();

        // Carriers start at their base points, the layout shoves them out of the way of growing zones.
        var carrierIds = new List<ProtoId<ReagentPrototype>>();
        var carriers = new List<Vector2>();
        foreach (var reagent in SortedById(_proto.EnumeratePrototypes<ReagentPrototype>()))
        {
            var point = reagent.AlchemyEffects.OfType<MoveToPoint>().FirstOrDefault();
            if (point == null)
                continue;

            carrierIds.Add(reagent.ID);
            carriers.Add(point.Pos);
        }

        var zoneIds = new List<ProtoId<CEAlchemyZonePrototype>>();
        var zones = new List<CEAlchemyLayout.Zone>();
        foreach (var zone in SortedById(_proto.EnumeratePrototypes<CEAlchemyZonePrototype>()))
        {
            zoneIds.Add(zone.ID);
            zones.Add(new CEAlchemyLayout.Zone { Weight = zone.Weight, Dangerous = zone.Dangerous });
        }

        CEAlchemyLayout.Generate(seed, zones, carriers);

        for (var i = 0; i < carrierIds.Count; i++)
            comp.Carriers[carrierIds[i]] = carriers[i];

        for (var i = 0; i < zoneIds.Count; i++)
        {
            comp.Zones[zoneIds[i]] = zones[i].Pos;
            comp.Radii[zoneIds[i]] = zones[i].Radius;
        }
    }

    private static List<T> SortedById<T>(IEnumerable<T> prototypes) where T : class, IPrototype
    {
        var list = prototypes.ToList();
        list.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID));
        return list;
    }

    /// <summary>
    /// Position of a carrier on the map of the round.
    /// </summary>
    public Vector2 GetCarrierPosition(ProtoId<ReagentPrototype> reagent, MoveToPoint point)
    {
        return GetLayout().Carriers.GetValueOrDefault(reagent, point.Pos);
    }

    /// <summary>
    /// Center of a zone on the map of the round.
    /// </summary>
    public Vector2 GetZonePosition(CEAlchemyZonePrototype zone)
    {
        return GetLayout().Zones.GetValueOrDefault(zone.ID);
    }

    /// <summary>
    /// Radius of a zone on the map of the round.
    /// </summary>
    public float GetZoneRadius(CEAlchemyZonePrototype zone)
    {
        return GetLayout().Radii.GetValueOrDefault(zone.ID);
    }

    /// <summary>
    /// Replays the recipe step by step: effects of every reagent move the marker, and every move
    /// may bring the path closer to the zone centers.
    /// </summary>
    /// <param name="trail">If given, receives the marker position at the start and after every step (for drawing the path).</param>
    public CEAlchemyState Replay(CEAlchemyData data, List<Vector2>? trail = null)
    {
        var state = new CEAlchemyState { ZoneDistances = new Dictionary<ProtoId<CEAlchemyZonePrototype>, float>() };
        List<(CEAlchemyZonePrototype Zone, Vector2 Center, float Radius)>? zones = null;
        trail?.Add(state.Pos);

        foreach (var step in data.Steps)
        {
            if (step is not CEAlchemyMixStep mix)
                continue;

            var amount = mix.Amount.Float();
            state.Volume += amount;

            if (!_proto.Resolve(mix.Reagent, out var reagent))
                continue;

            state.Power += amount * reagent.AlchemyPower;

            foreach (var effect in reagent.AlchemyEffects)
            {
                var before = state.Pos;
                effect.Apply(ref state, new CEAlchemyApplyArgs(mix.Reagent, amount, this));

                zones ??= GetZones();
                foreach (var (zone, center, _) in zones)
                {
                    var distance = DistanceToSegment(before, state.Pos, center);
                    if (!state.ZoneDistances.TryGetValue(zone.ID, out var closest) || distance < closest)
                        state.ZoneDistances[zone.ID] = distance;
                }
            }

            trail?.Add(state.Pos);
        }

        return state;
    }

    /// <summary>
    /// Finds which zones the recipe works through and how strongly, strongest first.
    /// The strength is the share given by how close the marker path came to the zone center
    /// (see <see cref="GetProximityShare"/>), multiplied by the power density and the tuning.
    /// </summary>
    public List<CEAlchemyHit> Analyze(CEAlchemyData data)
    {
        var hits = new List<CEAlchemyHit>();
        var state = Replay(data);

        if (state.Carriers <= 0 || state.Volume <= 0 || state.Power <= 0)
            return hits;

        var density = state.Power / state.Volume;
        var multiplier = data.Multiplier;

        foreach (var zone in _proto.EnumeratePrototypes<CEAlchemyZonePrototype>())
        {
            if (!state.ZoneDistances.TryGetValue(zone.ID, out var distance))
                continue;

            var share = GetProximityShare(distance, GetZoneRadius(zone));
            if (share <= 0)
                continue;

            var strength = share * density * multiplier;

            hits.Add(new CEAlchemyHit(zone, strength));
        }

        hits.Sort((a, b) => b.Strength.CompareTo(a.Strength));
        return hits;
    }

    private List<(CEAlchemyZonePrototype Zone, Vector2 Center, float Radius)> GetZones()
    {
        var zones = new List<(CEAlchemyZonePrototype, Vector2, float)>();
        foreach (var zone in _proto.EnumeratePrototypes<CEAlchemyZonePrototype>())
        {
            zones.Add((zone, GetZonePosition(zone), GetZoneRadius(zone)));
        }

        return zones;
    }

    /// <summary>
    /// Share of the zone effect for a path that came this close to the center: full inside the core,
    /// falling from the core to the edge in the outer ring, nothing outside the zone.
    /// </summary>
    public static float GetProximityShare(float distance, float radius)
    {
        if (distance >= radius)
            return 0;

        var core = radius * CoreRatio;
        if (distance <= core)
            return 1;

        return MathHelper.Lerp(RingInnerShare, RingOuterShare, (distance - core) / (radius - core));
    }

    /// <summary>
    /// Distance from the point to the closest point of the segment a-b.
    /// </summary>
    public static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 point)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        var t = lengthSquared < 1e-12f ? 0f : Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(a + ab * t, point);
    }
}
