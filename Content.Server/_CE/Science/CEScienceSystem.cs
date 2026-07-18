using Content.Server._CE.Science.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.Science;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem : CESharedScienceSystem
{
    private readonly EntProtoId _scienceEntity = "CEScience";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<CEScienceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        Spawn(_scienceEntity, MapCoordinates.Nullspace);
    }

    private void OnMapInit(Entity<CEScienceComponent> ent, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<CEScienceComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == ent.Owner)
                continue;

            QueueDel(ent.Owner);
            return;
        }
    }
}
