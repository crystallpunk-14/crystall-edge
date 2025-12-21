using System.Text;
using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Thief;
using Content.Shared.Chat;
using Content.Shared.Foldable;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server._CE.Thief;

public sealed partial class CEThiefSkillProgressionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly CESharedSkillSystem _skill = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly Color _messageColor = Color.FromSrgb(new Color(123, 173, 137));
    private readonly SoundSpecifier _newDaySound = new SoundPathSpecifier("/Audio/_CE/Announce/vampire.ogg");

    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<CETheftValueComponent> _theftValueQuery;
    private readonly HashSet<EntityUid> _countedItems = new();

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _theftValueQuery = GetEntityQuery<CETheftValueComponent>();

        SubscribeLocalEvent<CEThiefHideoutComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<CEThiefRoleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CEGlobalStartDayEvent>(OnStartDay);
    }

    private void OnStartDay(CEGlobalStartDayEvent ev)
    {
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mindComp))
        {
            if (!_role.MindHasRole<CEThiefRoleComponent>(mindId, out var thiefRole))
                continue;

            UpdateThiefSkillProgression(mindId, thiefRole);
        }
    }

    private void OnMapInit(Entity<CEThiefRoleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MaxScore = GetMaxScore();
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
            if (!_mind.TryGetMind(mindContainer, out var mindId, out var mindComp, mindContainer.Comp))
                continue;
            if (!_role.MindHasRole<CEThiefRoleComponent>(mindId))
                continue;

            ent.Comp.ThiefMind = mindId;
            return;
        }
    }

    private void UpdateThiefSkillProgression(EntityUid thiefMind, CEThiefRoleComponent thiefRole)
    {
        if (!TryComp<MindComponent>(thiefMind, out var mindComp))
            return;

        if (!TryComp<CESkillStorageComponent>(mindComp.OwnedEntity, out var skillStorage))
            return;

        var successPercentage = GetThiefSuccessPercentage(thiefMind, thiefRole);
        var currentScore = GetThiefScore(thiefMind);
        var maxSkillPoints = thiefRole.MaxSkillPointsFromStealing;
        var skillPointsToAward = maxSkillPoints * successPercentage;

        var skillPoints = skillStorage.SkillPoints;
        if (!skillPoints.TryGetValue(thiefRole.SkillPointType, out var currentPoints))
            return;

        var needAddSkillPoints = skillPointsToAward - currentPoints.Max;

        // Send chat messages
        if (_player.TryGetSessionById(mindComp.UserId, out var session))
        {
            var messageBuilder = new StringBuilder();
            var percentString = $"{successPercentage * 100:F1}%";
            messageBuilder.AppendLine(Loc.GetString("ce-thief-progression-new-day", ("percent", percentString)));

            // Check if this is a new record
            if (currentScore > thiefRole.PreviousBestScore)
            {
                var improvement = ((currentScore - thiefRole.PreviousBestScore) / thiefRole.MaxScore) * 100;
                var improvementString = $"{improvement:F1}%";
                messageBuilder.AppendLine(Loc.GetString("ce-thief-progression-record",
                    ("improvement", improvementString)));
                thiefRole.PreviousBestScore = currentScore;
            }

            // Add skill points info if any were awarded
            if (needAddSkillPoints > 0f)
            {
                var pointsString = $"{needAddSkillPoints:F1}";
                messageBuilder.AppendLine(Loc.GetString("ce-thief-progression-skill-gain", ("points", pointsString)));
                _skill.TryAddSkillPoints(mindComp.OwnedEntity.Value, thiefRole.SkillPointType, needAddSkillPoints);
            }

            var message = messageBuilder.ToString().TrimEnd();
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chat.ChatMessageToOne(ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                session.Channel,
                _messageColor);
            _audio.PlayEntity(_newDaySound, mindComp.OwnedEntity.Value, mindComp.OwnedEntity.Value);
        }
        else if (needAddSkillPoints > 0f)
        {
            // If no session but still need to add points
            _skill.TryAddSkillPoints(mindComp.OwnedEntity.Value, thiefRole.SkillPointType, needAddSkillPoints);
        }
    }

    private float GetThiefSuccessPercentage(EntityUid thiefMind, CEThiefRoleComponent thiefRole)
    {
        var thiefScore = GetThiefScore(thiefMind);
        var maxScore = thiefRole.MaxScore;

        if (maxScore <= 0f)
            return 0f;

        return Math.Clamp(thiefScore / maxScore, 0, 1);
    }

    private float GetThiefScore(EntityUid thiefMind)
    {
        if (!TryComp<MindComponent>(thiefMind, out var mindComp))
            return 0f;
        if (mindComp.OwnedEntity is null)
            return 0f;

        var score = 0f;

        _countedItems.Clear();

        // Calculate score from items in hideouts
        var query = EntityQueryEnumerator<CEThiefHideoutComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hideout, out var xform))
        {
            if (hideout.ThiefMind != thiefMind)
                continue;

            foreach (var item in
                     _lookup.GetEntitiesInRange<CETheftValueComponent>(xform.Coordinates, hideout.ScanRange))
            {
                if (_countedItems.Contains(item.Owner))
                    continue;

                score += item.Comp.Difficulty;
                _countedItems.Add(item.Owner);
            }
        }

        // Calculate score from items in thief's inventory
        var thief = mindComp.OwnedEntity.Value;

        // Recursively check all containers (inventory, bags, implants, etc.)
        if (!_containerQuery.TryGetComponent(thief, out var currentManager))
            return score;

        var containerStack = new Stack<ContainerManagerComponent>();

        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    // Check if this entity has theft value
                    if (_theftValueQuery.TryGetComponent(entity, out var theftValue) &&
                        !_countedItems.Contains(entity))
                    {
                        score += theftValue.Difficulty;
                        _countedItems.Add(entity);
                    }

                    // If it is a container, check its contents recursively
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

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
