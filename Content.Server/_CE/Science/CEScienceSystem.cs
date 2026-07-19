using Content.Server._CE.Science.Components;
using Content.Shared._CE.Science.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.EntityEffect;
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
    /// Marks an achievement as discovered for the given entity, networking the change. Returns
    /// false (and does not mutate anything) if it was already discovered.
    /// </summary>
    public bool DiscoverAchievement(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAchievementPrototype> achievement)
    {
        if (!ent.Comp.DiscoveredAchievements.Add(achievement))
            return false;

        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Un-marks an achievement as discovered for the given entity, networking the change.
    /// </summary>
    public void UndiscoverAchievement(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAchievementPrototype> achievement)
    {
        if (ent.Comp.DiscoveredAchievements.Remove(achievement))
            Dirty(ent);
    }

    /// <inheritdoc cref="CESharedScienceSystem.OnAchievementDiscovered"/>
    protected override void OnAchievementDiscovered(EntityUid user, ProtoId<CEScienceAchievementPrototype> achievement)
    {
        var data = EnsureComp<CEScienceResearchDataComponent>(user);
        TryDiscoverAchievement((user, data), achievement);
    }

    /// <summary>
    /// Marks an achievement as discovered (if not already), reveals a 3x3 area around wherever its
    /// cell is on the map, and applies its effects. This is the single path both the "discover
    /// achievement" research action and reading a physical achievement-holder item funnel through -
    /// callers are responsible for any cost of their own (e.g. the research action spending its
    /// points) before calling this.
    /// </summary>
    public bool TryDiscoverAchievement(Entity<CEScienceResearchDataComponent> ent, ProtoId<CEScienceAchievementPrototype> achievementId)
    {
        if (!_proto.TryIndex(achievementId, out var achievement))
            return false;

        if (!DiscoverAchievement(ent, achievementId))
            return false;

        if (TryGetSingleton(out var science) && science.Areas.TryGetValue(achievement.Area, out var areaCells))
        {
            foreach (var (coordinate, cell) in areaCells)
            {
                if (cell is not CEScienceAchievementCell achievementCell || achievementCell.Achievement != achievementId)
                    continue;

                RevealArea(ent, achievement.Area, coordinate, 1);
                break;
            }
        }

        var effectArgs = new CEEntityEffectArgs(EntityManager, ent.Owner, null, default, 0f, ent.Owner, null);
        foreach (var effect in achievement.Effects)
        {
            effect.Effect(effectArgs);
        }

        return true;
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
