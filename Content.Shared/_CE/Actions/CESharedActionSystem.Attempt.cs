using Content.Shared._CE.Actions.Components;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Power.Components;
using Content.Shared.SSDIndicator;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    private void InitializeAttempts()
    {

        SubscribeLocalEvent<CEActionManaCostComponent, ActionAttemptEvent>(OnManacostActionAttempt);
        SubscribeLocalEvent<CEActionStaminaCostComponent, ActionAttemptEvent>(OnStaminaCostActionAttempt);
        SubscribeLocalEvent<CEActionWeaponRequiredComponent, ActionAttemptEvent>(OnWeaponRequiredActionAttempt);

        SubscribeLocalEvent<CEActionSSDBlockComponent, ActionValidateEvent>(OnActionSSDAttempt);
    }

    /// <summary>
    /// Before using a spell, a mana check is made for the amount of mana to show warnings.
    /// </summary>
    private void OnManacostActionAttempt(Entity<CEActionManaCostComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<ActionComponent>(ent, out var action))
            return;

        //Total mana required
        var requiredMana = ent.Comp.ManaCost;

        if (ent.Comp.CanModifyManacost)
        {
            var manaEv = new CECalculateManacostEvent(args.User, ent.Comp.ManaCost);

            RaiseLocalEvent(args.User, manaEv);

            if (action.Container is not null)
                RaiseLocalEvent(action.Container.Value, manaEv);

            requiredMana = manaEv.TotalManacost;
        }

        //Trying get mana from performer
        if (!TryComp<BatteryComponent>(args.User, out var playerMana))
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-no-mana-component"), args.User, args.User);
            args.Cancelled = true;
            return;
        }

        if (playerMana.LastCharge < requiredMana)
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-not-enough-mana"), args.User, args.User);
            args.Cancelled = true;
        }
    }

    private void OnWeaponRequiredActionAttempt(Entity<CEActionWeaponRequiredComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_hand.TryGetActiveItem(args.User, out var held) &&
            HasComp<CEWeaponComponent>(held))
            return;

        Popup.PopupClient(Loc.GetString("ce-magic-weapon-required"), args.User, args.User);
        args.Cancelled = true;
    }

    private void OnActionSSDAttempt(Entity<CEActionSSDBlockComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        if (!TryComp<SSDIndicatorComponent>(GetEntity(args.Input.EntityTarget), out var ssdIndication))
            return;

        if (ssdIndication.IsSSD)
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-ssd"), args.User, args.User);
            args.Invalid = true;
        }
    }

    private void OnStaminaCostActionAttempt(Entity<CEActionStaminaCostComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<StaminaComponent>(args.User, out var staminaComp))
            return;

        if (staminaComp.CritThreshold - staminaComp.StaminaDamage < ent.Comp.Cost)
            args.Cancelled = true;
    }
}
