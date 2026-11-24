using Content.Server.GameTicking.Events;
using Content.Shared._CE.Alchemy.World;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Alchemy;

/// <summary>
/// Creates the round world at round start: lays out carriers and zones by the round seed (see CEAlchemyLayout).
/// </summary>
public sealed partial class CEAlchemyWorldServerSystem : EntitySystem
{
    private static readonly EntProtoId WorldPrototype = "CEAlchemyWorld";

    [Dependency] private CEAlchemySystem _alchemy = default!;
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
        _alchemy.FillWorld(world.Item2, _random.Next());
        Dirty(world.Item1, world.Item2);
        return world;
    }
}
