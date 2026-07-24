using Content.Shared._CE.Science.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    private void InitializeScientificInterest()
    {
        SubscribeLocalEvent<CEScientificInterestComponent, GetVerbsEvent<AlternativeVerb>>(AddScientificInterestVerbs);
        SubscribeLocalEvent<CEScientificInterestComponent, CEScientificInterestDoAfterEvent>(OnScientificInterestDoAfter);
    }

    private void AddScientificInterestVerbs(Entity<CEScientificInterestComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var alreadyStudied = ent.Comp.StudiedBy.Contains(user);

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ce-scientific-interest-verb-study"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            Act = () => StartInterestStudy(ent, user),
            Disabled = alreadyStudied,
            Message = alreadyStudied ? Loc.GetString("ce-scientific-interest-verb-already-studied") : null,
            Priority = 1,
        });
    }

    private bool StartInterestStudy(Entity<CEScientificInterestComponent> ent, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.Time, new CEScientificInterestDoAfterEvent(), ent, target: user, used: ent)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(user, ent),
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnScientificInterestDoAfter(Entity<CEScientificInterestComponent> ent, ref CEScientificInterestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!ent.Comp.StudiedBy.Add(target))
            return;

        Dirty(ent);

        var data = EnsureComp<CEScienceResearchDataComponent>(target);
        GrantPoints((target, data), ent.Comp.Points);

        _popup.PopupPredicted(
            Loc.GetString("ce-scientific-interest-popup-success", ("target", ent.Owner), ("points", ent.Comp.Points)),
            null,
            ent,
            target);
    }
}

[Serializable, NetSerializable]
public sealed partial class CEScientificInterestDoAfterEvent : SimpleDoAfterEvent;
