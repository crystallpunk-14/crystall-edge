using Content.Server._CE.Science.Components;
using Content.Shared._CE.Science.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.Knowledge;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem : CESharedScienceSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEKnowledgeSystem _knowledge = default!;

    private readonly EntProtoId _scienceEntity = "CEScience";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<CEScienceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEScienceResearchDataComponent, ComponentInit>(OnResearchDataInit);
        SubscribeLocalEvent<CEKnowledgeLearnedEvent>(OnKnowledgeLearned);
    }

    private void OnResearchDataInit(Entity<CEScienceResearchDataComponent> ent, ref ComponentInit args)
    {
        foreach (var area in _proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            RevealArea(ent, area.ID, default, 1);
        }
    }

    /// <summary>
    /// Marks a single coordinate as researched for the given area, networking the change.
    /// </summary>
    public void RevealCoordinate(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAreaPrototype> area, Vector2i coordinate)
    {
        if (!ent.Comp.Researched.TryGetValue(area, out var researched))
        {
            researched = new HashSet<Vector2i>();
            ent.Comp.Researched[area] = researched;
        }

        if (researched.Add(coordinate))
            Dirty(ent);
    }

    /// <summary>
    /// Marks every coordinate within <paramref name="radius"/> (inclusive, square) of
    /// <paramref name="center"/> as researched for the given area.
    /// </summary>
    public void RevealArea(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAreaPrototype> area, Vector2i center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
            RevealCoordinate(ent, area, center + new Vector2i(x, y));
    }

    /// <summary>
    /// Un-marks a single coordinate as researched for the given area.
    /// </summary>
    public void ClearCoordinate(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAreaPrototype> area, Vector2i coordinate)
    {
        if (ent.Comp.Researched.TryGetValue(area, out var researched) && researched.Remove(coordinate))
            Dirty(ent);
    }

    /// <summary>
    /// Un-marks an entire area as researched.
    /// </summary>
    public void ClearArea(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAreaPrototype> area)
    {
        if (ent.Comp.Researched.Remove(area))
            Dirty(ent);
    }

    /// <summary>
    /// Teaches the actor a discovery's linked knowledge - the single path both choosing a
    /// discovery's card and reading a physical knowledge-holder item funnel through. Callers are
    /// responsible for any cost of their own (e.g. spending the card's points) before calling this.
    /// Returns false if the knowledge was already known.
    /// </summary>
    public bool TryLearnDiscovery(EntityUid actor, ProtoId<CEScienceDiscoveryPrototype> discoveryId)
    {
        if (!_proto.TryIndex(discoveryId, out var discovery))
            return false;

        return _knowledge.TryLearn(actor, discovery.Knowledge);
    }

    /// <summary>
    /// Whenever an entity learns a piece of knowledge (from any source), reveals a 3x3 area
    /// around any discovery tile that teaches that same knowledge - this is what makes a
    /// discovered discovery's map icon render in full colour.
    /// </summary>
    private void OnKnowledgeLearned(ref CEKnowledgeLearnedEvent ev)
    {
        if (!TryGetSingleton(out var science))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(ev.Entity);

        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            if (discovery.Knowledge != ev.Knowledge)
                continue;

            if (!science.Areas.TryGetValue(discovery.Area, out var areaTiles))
                continue;

            foreach (var (coordinate, tile) in areaTiles)
            {
                if (tile is not CEScienceDiscoveryTile discoveryTile || discoveryTile.Discovery != discovery.ID)
                    continue;

                RevealArea((ev.Entity, data), discovery.Area, coordinate, 1);
                break;
            }
        }
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var uid = Spawn(_scienceEntity, MapCoordinates.Nullspace);

        if (!TryComp<CEScienceComponent>(uid, out var science))
            return;

        foreach (var area in _proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            science.Areas[area.ID] = GenerateArea(area);
        }

        InitializePools(science);
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
