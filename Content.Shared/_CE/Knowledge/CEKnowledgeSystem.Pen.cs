using Content.Shared._CE.Knowledge.Components;
using Content.Shared._CE.Pen;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Knowledge;

public sealed partial class CEKnowledgeSystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly ProtoId<TagPrototype> BookTag = "Book";

    private static readonly SpriteSpecifier RecordKnowledgeIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Paper/pen_interact_icons.rsi"), "reseaerch_write");

    private static readonly SoundSpecifier RecordKnowledgeSound = new SoundCollectionSpecifier("PaperScribbles");

    private void InitializePen()
    {
        SubscribeLocalEvent<CEKnowledgeComponent, CEGetPenActionsEvent>(OnGetPenActions);
        SubscribeLocalEvent<CEKnowledgeComponent, CEPenRecordKnowledgeRequestEvent>(OnRecordKnowledgeRequest);
        SubscribeLocalEvent<CEKnowledgeComponent, CEPenRecordKnowledgeDoAfterEvent>(OnRecordKnowledgeDoAfter);
    }

    private bool CanRecordKnowledge(EntityUid target)
    {
        return _tag.HasTag(target, BookTag) && !HasComp<CEKnowledgeHolderComponent>(target);
    }

    private void OnGetPenActions(Entity<CEKnowledgeComponent> ent, ref CEGetPenActionsEvent args)
    {
        if (ent.Owner != args.User || ent.Comp.Known.Count == 0)
            return;

        if (!CanRecordKnowledge(args.Target))
            return;

        args.Actions.Add(new CEPenAction(CEPenActionKind.RecordKnowledge, "ce-pen-action-record-knowledge", RecordKnowledgeIcon));
    }

    private void OnRecordKnowledgeRequest(Entity<CEKnowledgeComponent> ent, ref CEPenRecordKnowledgeRequestEvent args)
    {
        if (!ent.Comp.Known.Contains(args.Knowledge))
            return;

        if (!CanRecordKnowledge(args.Target))
            return;

        if (!_proto.TryIndex(args.Knowledge, out var knowledge))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, ReadTime, new CEPenRecordKnowledgeDoAfterEvent(args.Knowledge), ent, target: args.Target, used: args.Pen)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(ent.Owner, args.Pen),
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnRecordKnowledgeDoAfter(Entity<CEKnowledgeComponent> ent, ref CEPenRecordKnowledgeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!ent.Comp.Known.Contains(args.Knowledge) || !CanRecordKnowledge(target))
            return;

        if (!_proto.TryIndex(args.Knowledge, out var knowledge))
            return;

        var coordinates = Transform(target).Coordinates;

        PredictedQueueDel(target);

        var spawned = PredictedSpawnAtPosition(knowledge.Book, coordinates);

        // Knowledge is a required field read during the component's own MapInit handler (which
        // AddComp fires immediately, since the entity is already map-initialized) - it must be
        // set before the component is added, not after via EnsureComp.
        AddComp(spawned, new CEKnowledgeHolderComponent { Knowledge = args.Knowledge });

        _metaData.SetEntityName(spawned, Loc.GetString(knowledge.Name));

        var descLines = new List<string>();

        var effects = GetKnowledgeEffectDescription(args.Knowledge);
        if (effects.Length > 0)
            descLines.Add(effects);

        var authorName = MetaData(ent.Owner).EntityName;
        descLines.Add(Loc.GetString("ce-knowledge-book-author", ("name", authorName)));

        _metaData.SetEntityDescription(spawned, string.Join("\n", descLines));

        _audio.PlayPvs(RecordKnowledgeSound, spawned);
    }
}
