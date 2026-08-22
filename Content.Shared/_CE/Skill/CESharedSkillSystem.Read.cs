using Content.Shared._CE.Skill.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill;

public abstract partial class CESharedSkillSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly TimeSpan ReadTime = TimeSpan.FromSeconds(3);

    private void InitializeRead()
    {
        SubscribeLocalEvent<CESkillBookComponent, MapInitEvent>(OnBookMapInit);
        SubscribeLocalEvent<CESkillBookComponent, UseInHandEvent>(OnBookUseInHand, after: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<CESkillBookComponent, GetVerbsEvent<AlternativeVerb>>(AddBookVerbs);
        SubscribeLocalEvent<CESkillBookComponent, CESkillReadDoAfterEvent>(OnBookDoAfter);
    }

    private void OnBookMapInit(Entity<CESkillBookComponent> ent, ref MapInitEvent args)
    {
        if (!_proto.HasIndex(ent.Comp.Skill))
            return;

        _metaData.SetEntityName(ent, GetSkillName(ent.Comp.Skill));

        var effects = GetSkillEffectDescription(ent.Comp.Skill);
        if (effects.Length > 0)
            _metaData.SetEntityDescription(ent, effects);
    }

    private void OnBookUseInHand(Entity<CESkillBookComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartRead(ent, args.User);
    }

    private void AddBookVerbs(Entity<CESkillBookComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ce-skill-verb-study"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            Act = () => StartRead(ent, user),
            Priority = 1,
        });
    }

    private bool StartRead(Entity<CESkillBookComponent> ent, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ReadTime, new CESkillReadDoAfterEvent(), ent, target: user, used: ent)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(user, ent),
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnBookDoAfter(Entity<CESkillBookComponent> ent, ref CESkillReadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!SkillConditionsMet(target, ent.Comp.Skill))
        {
            _popup.PopupClient(Loc.GetString("ce-skill-req-not-met"), target, target);
            return;
        }

        if (TryAddSkill(target, ent.Comp.Skill))
            _popup.PopupClient(Loc.GetString("ce-skill-learned"), target, target);
        else
            _popup.PopupClient(Loc.GetString("ce-skill-already-known"), target, target);
    }
}
