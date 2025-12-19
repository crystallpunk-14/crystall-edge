using Content.Server.Roles;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Thief;
using Content.Shared.Foldable;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server._CE.Thief;

public sealed partial class CEThiefSkillProgressionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly CESharedSkillSystem _skill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThiefHideoutComponent, FoldedEvent>(OnFolded);
    }

    /// <summary>
    /// When spawning, we look for the nearest thief player and try to attach ourselves to them.
    /// </summary>
    private void OnFolded(Entity<CEThiefHideoutComponent> ent, ref FoldedEvent args)
    {
        if (args.IsFolded || ent.Comp.ThiefMind is not null)
            return;

        var minds = _lookup.GetEntitiesInRange<MindContainerComponent>(Transform(ent).Coordinates, ent.Comp.ScanRange);

        foreach (var mindContainer in minds)
        {
            if(!_mind.TryGetMind(mindContainer, out var mindId, out var mindComp, mindContainer.Comp))
                continue;
            if (!_role.MindHasRole<CEThiefRoleComponent>(mindId))
                continue;

            ent.Comp.ThiefMind = mindId;
            return;
        }
    }

    private void UpdateThiefSkillProgression(EntityUid thiefMind)
    {
        if (!_mind.TryGetMind(thiefMind, out var mindId, out var mindComp))
            return;

        if (!TryComp<CESkillStorageComponent>(mindComp.OwnedEntity, out var skillStorage))
            return;

        if (!_role.MindHasRole<CEThiefRoleComponent>(thiefMind, out var thiefRole))
            return;

        var successPercentage = GetThiefSuccessPercentage(thiefMind);
        var maxSkillPoints = thiefRole.Value.Comp2.MaxSkillPointsFromStealing;
        var skillPointsToAward = maxSkillPoints * successPercentage;

        var skillPoints = skillStorage.SkillPoints;
        if (!skillPoints.TryGetValue(thiefRole.Value.Comp2.SkillPointType, out var currentPoints))
            return;

        var needAddSkillPoints = skillPointsToAward - currentPoints.Max;

        if (needAddSkillPoints <= 0f)
            return;

        _skill.TryAddSkillPoints(mindComp.OwnedEntity.Value, thiefRole.Value.Comp2.SkillPointType, needAddSkillPoints);
    }

    private float GetThiefSuccessPercentage(EntityUid thiefMind)
    {
        var thiefScore = GetThiefScore(thiefMind);
        var maxScore = GetMaxScore();

        if (maxScore <= 0f)
            return 0f;

        return thiefScore / maxScore;
    }

    private float GetThiefScore(EntityUid thiefMind)
    {
        var query = EntityQueryEnumerator<CEThiefHideoutComponent, TransformComponent>();

        var score = 0f;
        while (query.MoveNext(out var uid, out var hideout, out var xform))
        {
            if (hideout.ThiefMind != thiefMind)
                continue;

            foreach (var item in _lookup.GetEntitiesInRange<CETheftValueComponent>(xform.Coordinates, hideout.ScanRange))
            {
                score += item.Comp.Difficulty;
            }
        }

        return score;
    }

    private float GetMaxScore()
    {
        var score = 0f;
        var query = EntityQueryEnumerator<CETheftValueComponent>();
        while (query.MoveNext(out var uid, out var theftValue))
        {
            score += theftValue.Difficulty;
        }

        return score;
    }
}
