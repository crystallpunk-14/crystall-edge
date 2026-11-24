using System.Linq;
using System.Numerics;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.Alchemy.Effects;
using Content.Shared._CE.Alchemy.Prototypes;
using Content.Shared._CE.Alchemy.World;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Alchemy;

/// <summary>
/// Creates the round world at round start: shifts carriers and zones by the round seed, checking every shift in place.
/// </summary>
public sealed partial class CEAlchemyWorldServerSystem : EntitySystem
{
    private static readonly EntProtoId WorldPrototype = "CEAlchemyWorld";

    private const float CarrierShift = 0.05f;
    private const float ZoneShift = 0.1f;
    private const int ZoneAttempts = 80;

    /// <summary>
    /// Zones must fit inside this radius of the map.
    /// </summary>
    private const float MapRadius = 0.97f;

    private const float CarrierMargin = 0.04f;
    private const float ZoneMargin = 0.03f;

    [Dependency] private CEAlchemySystem _alchemy = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        EnsureWorld();
    }

    public Entity<CEAlchemyWorldComponent> EnsureWorld()
    {
        if (_alchemy.TryGetWorld(out var existing))
            return existing;

        var uid = Spawn(WorldPrototype, MapCoordinates.Nullspace);
        var world = (uid, EnsureComp<CEAlchemyWorldComponent>(uid));
        Generate(world, _random.Next());
        return world;
    }

    private void Generate(Entity<CEAlchemyWorldComponent> world, int seed)
    {
        var rand = new System.Random(seed);
        var comp = world.Comp;
        comp.Seed = seed;
        comp.Carriers.Clear();
        comp.Zones.Clear();

        // Carriers shift freely, zones then avoid them.
        var carriers = new List<Vector2>();
        foreach (var reagent in SortedById(_proto.EnumeratePrototypes<ReagentPrototype>()))
        {
            var point = reagent.AlchemyEffects.OfType<CoordinatePoint>().FirstOrDefault();
            if (point == null)
                continue;

            var pos = point.Pos + Jitter(rand, CarrierShift);
            comp.Carriers[reagent.ID] = pos;
            carriers.Add(pos);
        }

        var placed = new List<(Vector2 Center, float Radius)>();
        foreach (var zone in SortedById(_proto.EnumeratePrototypes<CEAlchemyZonePrototype>()))
        {
            var center = zone.Position;
            for (var i = 0; i < ZoneAttempts; i++)
            {
                var amplitude = i < ZoneAttempts / 2 ? ZoneShift : ZoneShift / 2;
                var candidate = zone.Position + Jitter(rand, amplitude);
                if (!ZoneFits(candidate, zone.Radius, carriers, placed))
                    continue;

                center = candidate;
                break;
            }

            comp.Zones[zone.ID] = center;
            placed.Add((center, zone.Radius));
        }

        Dirty(world.Owner, comp);
    }

    private static bool ZoneFits(Vector2 center,
        float radius,
        List<Vector2> carriers,
        List<(Vector2 Center, float Radius)> placed)
    {
        if (center.Length() + radius > MapRadius)
            return false;

        foreach (var carrier in carriers)
        {
            if (Vector2.Distance(carrier, center) < radius + CarrierMargin)
                return false;
        }

        foreach (var (otherCenter, otherRadius) in placed)
        {
            if (Vector2.Distance(otherCenter, center) < radius + otherRadius + ZoneMargin)
                return false;
        }

        return true;
    }

    private static Vector2 Jitter(System.Random rand, float amplitude)
    {
        return new Vector2((float) (rand.NextDouble() * 2 - 1), (float) (rand.NextDouble() * 2 - 1)) * amplitude;
    }

    private static List<T> SortedById<T>(IEnumerable<T> prototypes) where T : class, IPrototype
    {
        var list = prototypes.ToList();
        list.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID));
        return list;
    }
}
