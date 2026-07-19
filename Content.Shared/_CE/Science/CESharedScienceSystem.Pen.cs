using Content.Shared._CE.Pen;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly ProtoId<TagPrototype> BookTag = "Book";

    private static readonly SpriteSpecifier RecordKnowledgeIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Paper/pen_interact_icons.rsi"), "reseaerch_write");

    private static readonly SoundSpecifier RecordKnowledgeSound = new SoundCollectionSpecifier("PaperScribbles");

    private void InitializePen()
    {
        SubscribeLocalEvent<CEScienceResearchDataComponent, CEGetPenActionsEvent>(OnGetPenActions);
        SubscribeLocalEvent<CEScienceResearchDataComponent, CEPenRecordKnowledgeRequestEvent>(OnRecordKnowledgeRequest);
        SubscribeLocalEvent<CEScienceResearchDataComponent, CEPenRecordKnowledgeDoAfterEvent>(OnRecordKnowledgeDoAfter);
    }

    private bool CanRecordKnowledge(EntityUid target)
    {
        return _tag.HasTag(target, BookTag) && !HasComp<CEScienceAchievementHolderComponent>(target);
    }

    private void OnGetPenActions(Entity<CEScienceResearchDataComponent> ent, ref CEGetPenActionsEvent args)
    {
        if (ent.Owner != args.User || ent.Comp.DiscoveredAchievements.Count == 0)
            return;

        if (!CanRecordKnowledge(args.Target))
            return;

        args.Actions.Add(new CEPenAction(CEPenActionKind.RecordKnowledge, "ce-pen-action-record-knowledge", RecordKnowledgeIcon));
    }

    private void OnRecordKnowledgeRequest(Entity<CEScienceResearchDataComponent> ent, ref CEPenRecordKnowledgeRequestEvent args)
    {
        if (!ent.Comp.DiscoveredAchievements.Contains(args.Achievement))
            return;

        if (!CanRecordKnowledge(args.Target))
            return;

        if (!_proto.TryIndex(args.Achievement, out var achievement))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, achievement.Time, new CEPenRecordKnowledgeDoAfterEvent(args.Achievement), ent, target: args.Target, used: args.Pen)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(ent.Owner, args.Pen),
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnRecordKnowledgeDoAfter(Entity<CEScienceResearchDataComponent> ent, ref CEPenRecordKnowledgeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!ent.Comp.DiscoveredAchievements.Contains(args.Achievement) || !CanRecordKnowledge(target))
            return;

        if (!_proto.TryIndex(args.Achievement, out var achievement) || !_proto.TryIndex(achievement.Area, out var area))
            return;

        var coordinates = Transform(target).Coordinates;

        PredictedQueueDel(target);

        var spawned = PredictedSpawnAtPosition(area.Book, coordinates);

        // Achievement is a required field read during the component's own MapInit handler
        // (which AddComp fires immediately, since the entity is already map-initialized) - it
        // must be set before the component is added, not after via EnsureComp.
        AddComp(spawned, new CEScienceAchievementHolderComponent { Achievement = args.Achievement });

        _metaData.SetEntityName(spawned, Loc.GetString(achievement.Name));
        if (achievement.Desc is { } desc)
            _metaData.SetEntityDescription(spawned, Loc.GetString(desc));

        _audio.PlayPvs(RecordKnowledgeSound, spawned);
    }
}