using System.Linq;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CEMagicEssenceSystem _essence = default!;

    private void InitializeScientificInterest()
    {
        SubscribeLocalEvent<CEScientificInterestComponent, GetVerbsEvent<AlternativeVerb>>(AddScientificInterestVerbs);
        SubscribeLocalEvent<CEScientificInterestComponent, CEScientificInterestDoAfterEvent>(OnScientificInterestDoAfter);
        SubscribeLocalEvent<CEScienceRandomPointsComponent, MapInitEvent>(OnRandomPointsMapInit);
    }

    /// <summary>
    /// Rolls a random set of research points into this entity's <see cref="CEScientificInterestComponent"/>
    /// (added if missing), weighted towards low-tier essences - see <see cref="CEMagicEssenceSystem.GetRandomEssenceType"/>.
    /// Only the server rolls; the result reaches the client via <see cref="CEScientificInterestComponent"/>'s
    /// own networked state, same as every other server-authoritative roll in this game.
    /// </summary>
    private void OnRandomPointsMapInit(Entity<CEScienceRandomPointsComponent> ent, ref MapInitEvent args)
    {
        if (!_net.IsServer)
            return;

        var interest = EnsureComp<CEScientificInterestComponent>(ent.Owner);
        interest.Points.Clear();

        for (var i = 0; i < ent.Comp.RollCount; i++)
        {
            var essence = _essence.GetRandomEssenceType();
            var amount = _random.Next(ent.Comp.MinAmount, ent.Comp.MaxAmount + 1);
            interest.Points[essence] = interest.Points.GetValueOrDefault(essence) + amount;
        }

        Dirty(ent.Owner, interest);
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

        var pointsText = string.Join(", ", ent.Comp.Points.Select(kv =>
        {
            var essenceName = _proto.TryIndex(kv.Key, out var essence) ? essence.Name : kv.Key.Id;
            return $"{kv.Value} {essenceName}";
        }));

        _popup.PopupPredicted(
            Loc.GetString("ce-scientific-interest-popup-success", ("target", ent.Owner), ("points", pointsText)),
            null,
            ent,
            target);
    }
}

[Serializable, NetSerializable]
public sealed partial class CEScientificInterestDoAfterEvent : SimpleDoAfterEvent;
