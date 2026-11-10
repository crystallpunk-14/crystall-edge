using Content.Shared._CE.EntityEffect;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Consumable;

/// <summary>
/// Applies <see cref="CEConsumableComponent"/> items to oneself or another.
/// </summary>
public sealed partial class CEConsumableSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<CEConsumableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.AllowSelf)
            return;

        args.Handled = TryStart(ent, args.User, args.User);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<CEConsumableComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        args.Handled = TryStart(ent, args.User, target);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CEConsumableComponent> ent, ref CEConsumableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!CanApply(ent, args.User, target, popup: true))
            return;

        args.Handled = true;
        var itemName = ItemName(ent);

        var effectArgs = MakeArgs(ent, args.User, target);
        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }

        _adminLog.Add(LogType.InteractUsing,
            $"{ToPrettyString(args.User):user} applied {ToPrettyString(ent):used} to {ToPrettyString(target):target}");

        _audio.PlayPredicted(ent.Comp.EndSound, target, args.User);

        var remains = false;
        if (TryComp<StackComponent>(ent, out var stack))
        {
            _stack.ReduceCount((ent, stack), 1);
            remains = _stack.GetCount((ent, stack)) > 0;
        }
        else
        {
            PredictedQueueDel(ent.Owner);
        }

        args.Repeat = ent.Comp.Repeat && remains && ConditionsPass(ent, args.User, target);
        if (!args.Repeat)
            _popup.PopupClient(Loc.GetString("ce-consumable-finished", ("item", itemName)), target, args.User);
    }

    /// <summary>
    /// Starts applying the item to the target. Returns false when the target is not valid for this item at all.
    /// </summary>
    public bool TryStart(Entity<CEConsumableComponent> ent, EntityUid user, EntityUid target)
    {
        var self = user == target;
        if (self ? !ent.Comp.AllowSelf : !ent.Comp.AllowOthers)
            return false;

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, target))
            return false;

        if (!CanApply(ent, user, target, popup: true))
            return true;

        if (!self && !_interaction.InRangeUnobstructed(user, target, popup: true))
            return true;

        _audio.PlayPredicted(ent.Comp.BeginSound, ent, user);

        if (!self)
        {
            var msg = Loc.GetString("ce-consumable-popup-target",
                ("user", FormattedMessage.RemoveMarkupPermissive(Identity.Name(user, EntityManager))),
                ("item", ItemName(ent)));
            _popup.PopupEntity(msg, target, target, PopupType.Medium);
        }

        var delay = self ? ent.Comp.Delay * ent.Comp.SelfDelayMultiplier : ent.Comp.Delay;
        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new CEConsumableDoAfterEvent(), ent, target: target, used: ent)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        return true;
    }

    private bool CanApply(Entity<CEConsumableComponent> ent, EntityUid user, EntityUid target, bool popup)
    {
        if (TryComp<StackComponent>(ent, out var stack) && _stack.GetCount((ent, stack)) < 1)
            return false;

        if (ConditionsPass(ent, user, target))
            return true;

        if (popup)
            _popup.PopupClient(Loc.GetString(ent.Comp.FailPopup, ("item", ItemName(ent))), target, user);

        return false;
    }

    private bool ConditionsPass(Entity<CEConsumableComponent> ent, EntityUid user, EntityUid target)
    {
        var effectArgs = MakeArgs(ent, user, target);
        foreach (var condition in ent.Comp.Conditions)
        {
            if (!condition.Passes(effectArgs))
                return false;
        }

        return true;
    }

    private CEEntityEffectArgs MakeArgs(EntityUid used, EntityUid user, EntityUid target)
    {
        return new CEEntityEffectArgs(EntityManager, user, used, Angle.Zero, 0f, target, null);
    }

    private string ItemName(EntityUid item)
    {
        return FormattedMessage.RemoveMarkupPermissive(Name(item));
    }
}

[Serializable, NetSerializable]
public sealed partial class CEConsumableDoAfterEvent : SimpleDoAfterEvent;
