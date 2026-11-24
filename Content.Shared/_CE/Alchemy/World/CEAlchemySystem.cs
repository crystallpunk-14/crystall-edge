using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
}
