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
    /// Position of a carrier on the map of the round, or the base position from its prototype.
    /// </summary>
    public Vector2 GetCarrierPosition(ProtoId<ReagentPrototype> reagent, CoordinatePoint point)
    {
        if (TryGetWorld(out var world) && world.Comp.Carriers.TryGetValue(reagent, out var pos))
            return pos;

        return point.Pos;
    }

    /// <summary>
    /// Center of a zone on the map of the round, or the base position from its prototype.
    /// </summary>
    public Vector2 GetZonePosition(CEAlchemyZonePrototype zone)
    {
        if (TryGetWorld(out var world) && world.Comp.Zones.TryGetValue(zone.ID, out var pos))
            return pos;

        return zone.Position;
    }

    /// <summary>
    /// Replays the recipe step by step: effects of every reagent move the marker, and every move
    /// adds its length inside the <see cref="CEAlchemyZonePrototype.ByPath"/> zones.
    /// </summary>
    public CEAlchemyState Replay(CEAlchemyData data)
    {
        var state = new CEAlchemyState { ZoneLengths = new Dictionary<ProtoId<CEAlchemyZonePrototype>, float>() };
        List<(CEAlchemyZonePrototype Zone, Vector2 Center)>? pathZones = null;

        foreach (var step in data.Steps)
        {
            if (step is not CEAlchemyMixStep mix)
                continue;

            var amount = mix.Amount.Float();
            state.Volume += amount;

            if (!_proto.Resolve(mix.Reagent, out var reagent))
                continue;

            foreach (var effect in reagent.AlchemyEffects)
            {
                var before = state.Pos;
                effect.Apply(ref state, new CEAlchemyApplyArgs(mix.Reagent, amount, this));

                pathZones ??= GetPathZones();
                foreach (var (zone, center) in pathZones)
                {
                    var length = Chord(before, state.Pos, center, zone.Radius);
                    if (length > 0)
                        state.ZoneLengths[zone.ID] = state.ZoneLengths.GetValueOrDefault(zone.ID) + length;
                }
            }
        }

        return state;
    }

    private List<(CEAlchemyZonePrototype Zone, Vector2 Center)> GetPathZones()
    {
        var zones = new List<(CEAlchemyZonePrototype, Vector2)>();
        foreach (var zone in _proto.EnumeratePrototypes<CEAlchemyZonePrototype>())
        {
            if (zone.ByPath)
                zones.Add((zone, GetZonePosition(zone)));
        }

        return zones;
    }

    /// <summary>
    /// Length of the part of the segment a-b inside the circle.
    /// </summary>
    private static float Chord(Vector2 a, Vector2 b, Vector2 center, float radius)
    {
        var d = b - a;
        var f = a - center;
        var aa = d.LengthSquared();
        if (aa < 1e-12f)
            return 0;

        var bb = 2 * Vector2.Dot(f, d);
        var cc = f.LengthSquared() - radius * radius;
        var discriminant = bb * bb - 4 * aa * cc;
        if (discriminant <= 0)
            return 0;

        var root = MathF.Sqrt(discriminant);
        var t1 = MathF.Max(0, (-bb - root) / (2 * aa));
        var t2 = MathF.Min(1, (-bb + root) / (2 * aa));
        return t2 > t1 ? (t2 - t1) * MathF.Sqrt(aa) : 0;
    }
}
