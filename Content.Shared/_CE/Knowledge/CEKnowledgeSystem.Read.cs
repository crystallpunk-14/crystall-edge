using Content.Shared._CE.Knowledge.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Knowledge;

public sealed partial class CEKnowledgeSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly TimeSpan ReadTime = TimeSpan.FromSeconds(3);

    private void InitializeRead()
    {
        SubscribeLocalEvent<CEKnowledgeHolderComponent, MapInitEvent>(OnHolderMapInit);
        SubscribeLocalEvent<CEKnowledgeHolderComponent, UseInHandEvent>(OnHolderUseInHand, after: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<CEKnowledgeHolderComponent, GetVerbsEvent<AlternativeVerb>>(AddHolderVerbs);
        SubscribeLocalEvent<CEKnowledgeHolderComponent, CEKnowledgeReadDoAfterEvent>(OnHolderDoAfter);
    }

    private void OnHolderMapInit(Entity<CEKnowledgeHolderComponent> ent, ref MapInitEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.Knowledge, out var knowledge))
            return;

        _metaData.SetEntityName(ent, knowledge.GetTitle(_proto));

        var effects = GetKnowledgeEffectDescription(ent.Comp.Knowledge);
        if (effects.Length > 0)
            _metaData.SetEntityDescription(ent, effects);
    }

    private void OnHolderUseInHand(Entity<CEKnowledgeHolderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartRead(ent, args.User);
    }

    private void AddHolderVerbs(Entity<CEKnowledgeHolderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ce-knowledge-verb-study"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            Act = () => StartRead(ent, user),
            Priority = 1,
        });
    }

    private bool StartRead(Entity<CEKnowledgeHolderComponent> ent, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ReadTime, new CEKnowledgeReadDoAfterEvent(), ent, target: user, used: ent)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(user, ent),
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnHolderDoAfter(Entity<CEKnowledgeHolderComponent> ent, ref CEKnowledgeReadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (TryLearn(target, ent.Comp.Knowledge))
            _popup.PopupClient(Loc.GetString("ce-knowledge-learned"), target, target);
        else
            _popup.PopupClient(Loc.GetString("ce-knowledge-already-known"), target, target);
    }
}
