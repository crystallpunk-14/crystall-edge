using Content.Server._CE.Science.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem : CESharedScienceSystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly EntProtoId _scienceEntity = "CEScience";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<CEScienceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEScienceResearchDataComponent, ComponentInit>(OnResearchDataInit);
    }

    /// <summary>
    /// Grants the (-1,-1) to (1,1) square for every currently existing science area as soon as
    /// an entity gets its research data component.
    /// </summary>
    private void OnResearchDataInit(Entity<CEScienceResearchDataComponent> ent, ref ComponentInit args)
    {
        foreach (var area in _proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            var square = new HashSet<Vector2i>();
            for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                square.Add(new Vector2i(x, y));

            ent.Comp.Researched[area.ID] = square;
        }
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var uid = Spawn(_scienceEntity, MapCoordinates.Nullspace);
        GenerateHardcodedMap(uid);
    }

    /// <summary>
    /// Temporary hardcoded map generation, standing in for real procedural generation.
    /// </summary>
    private void GenerateHardcodedMap(EntityUid uid)
    {
        if (!TryComp<CEScienceComponent>(uid, out var science))
            return;

        science.Areas["ArcaneEngineering"] = new Dictionary<Vector2i, CEScienceMapCell>
        {
            [new Vector2i(3, -4)] = new CEScienceAchievementCell("Hoverboards"),
            [new Vector2i(2, -4)] = new CEScienceDeadZoneCell(),
            [new Vector2i(4, -3)] = new CEScienceDeadZoneCell(),
            [new Vector2i(1, -5)] = new CEScienceDeadZoneCell(),
            [new Vector2i(5, -5)] = new CEScienceDeadZoneCell(),
            [new Vector2i(3, -6)] = new CEScienceDeadZoneCell(),
        };
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

    /// <summary>
    /// Resolves the singleton science entity's data component, if it has been spawned this round.
    /// </summary>
    public bool TryGetSingleton(out CEScienceComponent science)
    {
        var query = EntityQueryEnumerator<CEScienceComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            science = comp;
            return true;
        }

        science = default!;
        return false;
    }
}
