using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Placeable;

/// <summary>
/// Takes <see cref="CETakeDownComponent"/> entities down into the user's hand, by an empty-hand click or a verb.
/// </summary>
public sealed partial class CETakeDownSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<CETakeDownComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartTakeDown(ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<CETakeDownComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ce-take-down-verb"),
            Act = () => TryStartTakeDown(ent, user),
        });
    }

    [SubscribeLocalEvent]
    private void OnTakeDownDoAfter(Entity<CETakeDownComponent> ent, ref CETakeDownDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        // Spawned on the server only, same as the placed entity itself.
        if (_net.IsClient)
            return;

        var item = Spawn(ent.Comp.Item, Transform(ent).Coordinates);
        QueueDel(ent);
        _hands.PickupOrDrop(args.User, item);
    }

    private bool TryStartTakeDown(Entity<CETakeDownComponent> ent, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, new CETakeDownDoAfterEvent(), ent, target: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }
}

[Serializable, NetSerializable]
public sealed partial class CETakeDownDoAfterEvent : SimpleDoAfterEvent;
