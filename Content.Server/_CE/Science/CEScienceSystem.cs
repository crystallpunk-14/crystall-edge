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
            RevealArea(ent, area.ID, default, 0);
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
    /// Teaches the actor an achievement's linked knowledge - the single path both the "discover
    /// achievement" research action and reading a physical knowledge-holder item funnel through.
    /// Callers are responsible for any cost of their own (e.g. the research action spending its
    /// points) before calling this. Returns false if the knowledge was already known.
    /// </summary>
    public bool TryDiscoverAchievement(EntityUid actor, ProtoId<CEScienceAchievementPrototype> achievementId)
    {
        if (!_proto.TryIndex(achievementId, out var achievement))
            return false;

        return _knowledge.TryLearn(actor, achievement.Knowledge);
    }

    /// <summary>
    /// Whenever an entity learns a piece of knowledge (from any source), reveals a 3x3 area
    /// around any achievement cell that teaches that same knowledge - this is what makes a
    /// discovered achievement's map icon render in full colour.
    /// </summary>
    private void OnKnowledgeLearned(ref CEKnowledgeLearnedEvent ev)
    {
        if (!TryGetSingleton(out var science))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(ev.Entity);

        foreach (var achievement in _proto.EnumeratePrototypes<CEScienceAchievementPrototype>())
        {
            if (achievement.Knowledge != ev.Knowledge)
                continue;

            if (!science.Areas.TryGetValue(achievement.Area, out var areaCells))
                continue;

            foreach (var (coordinate, cell) in areaCells)
            {
                if (cell is not CEScienceAchievementCell achievementCell || achievementCell.Achievement != achievement.ID)
                    continue;

                RevealArea((ev.Entity, data), achievement.Area, coordinate, 1);
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
