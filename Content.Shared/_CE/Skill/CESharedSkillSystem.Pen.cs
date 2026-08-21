using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Pen;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill;

public abstract partial class CESharedSkillSystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly ProtoId<TagPrototype> BookTag = "Book";

    private static readonly SpriteSpecifier RecordSkillIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Paper/pen_interact_icons.rsi"), "reseaerch_write");

    private static readonly SoundSpecifier RecordSkillSound = new SoundCollectionSpecifier("PaperScribbles");

    private void InitializePen()
    {
        SubscribeLocalEvent<CESkillStorageComponent, CEGetPenActionsEvent>(OnGetPenActions);
        SubscribeLocalEvent<CESkillStorageComponent, CEPenRecordSkillRequestEvent>(OnRecordSkillRequest);
        SubscribeLocalEvent<CESkillStorageComponent, CEPenRecordSkillDoAfterEvent>(OnRecordSkillDoAfter);
    }

    private bool CanRecordSkill(EntityUid target)
    {
        return _tag.HasTag(target, BookTag) && !HasComp<CESkillBookComponent>(target);
    }

    private void OnGetPenActions(Entity<CESkillStorageComponent> ent, ref CEGetPenActionsEvent args)
    {
        if (ent.Owner != args.User || ent.Comp.LearnedSkills.Count == 0)
            return;

        if (!CanRecordSkill(args.Target))
            return;

        args.Actions.Add(new CEPenAction(CEPenActionKind.RecordSkill, "ce-pen-action-record-skill", RecordSkillIcon));
    }

    private void OnRecordSkillRequest(Entity<CESkillStorageComponent> ent, ref CEPenRecordSkillRequestEvent args)
    {
        if (!ent.Comp.LearnedSkills.Contains(args.Skill))
            return;

        if (!CanRecordSkill(args.Target))
            return;

        if (!_proto.HasIndex(args.Skill))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, ReadTime, new CEPenRecordSkillDoAfterEvent(args.Skill), ent, target: args.Target, used: args.Pen)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(ent.Owner, args.Pen),
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnRecordSkillDoAfter(Entity<CESkillStorageComponent> ent, ref CEPenRecordSkillDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!ent.Comp.LearnedSkills.Contains(args.Skill) || !CanRecordSkill(target))
            return;

        if (!_proto.TryIndex(args.Skill, out var skill))
            return;

        var coordinates = Transform(target).Coordinates;

        PredictedQueueDel(target);

        var spawned = PredictedSpawnAtPosition(skill.Book, coordinates);

        // Skill is a required field read during the component's own MapInit handler (which
        // AddComp fires immediately, since the entity is already map-initialized) - it must be
        // set before the component is added, not after via EnsureComp.
        AddComp(spawned, new CESkillBookComponent { Skill = args.Skill });

        _metaData.SetEntityName(spawned, GetSkillName(args.Skill));

        var descLines = new List<string>();

        var effects = GetSkillEffectDescription(args.Skill);
        if (effects.Length > 0)
            descLines.Add(effects);

        var authorName = MetaData(ent.Owner).EntityName;
        descLines.Add(Loc.GetString("ce-skill-book-author", ("name", authorName)));

        _metaData.SetEntityDescription(spawned, string.Join("\n", descLines));

        _audio.PlayPvs(RecordSkillSound, spawned);
    }
}
