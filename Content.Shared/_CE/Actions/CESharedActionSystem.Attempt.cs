using System.Linq;
using Content.Shared._CE.Actions.Components;
using Content.Shared._CE.Actions.Events;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.SSDIndicator;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    private void InitializeAttempts()
    {
        SubscribeLocalEvent<CEActionFreeHandsRequiredComponent, ActionAttemptEvent>(OnSomaticActionAttempt);
        SubscribeLocalEvent<CEActionSpeakingComponent, ActionAttemptEvent>(OnVerbalActionAttempt);
        SubscribeLocalEvent<CEActionMaterialCostComponent, ActionAttemptEvent>(OnMaterialActionAttempt);
        SubscribeLocalEvent<CEActionManaCostComponent, ActionAttemptEvent>(OnManacostActionAttempt);
        SubscribeLocalEvent<CEActionStaminaCostComponent, ActionAttemptEvent>(OnStaminaCostActionAttempt);
        SubscribeLocalEvent<CEActionDangerousComponent, ActionAttemptEvent>(OnDangerousActionAttempt);

        SubscribeLocalEvent<CEActionSSDBlockComponent, ActionValidateEvent>(OnActionSSDAttempt);
        SubscribeLocalEvent<CEActionTargetMobStatusRequiredComponent, ActionValidateEvent>(OnTargetMobStatusRequiredValidate);
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

            requiredMana = manaEv.GetManacost();
        }

        //First - trying get mana from item
        if (action.Container is not null && TryComp<BatteryComponent>(action.Container, out var battery))
            requiredMana = MathF.Max(0, (float)(requiredMana - battery.CurrentCharge));

        if (requiredMana <= 0)
            return;

        //Second - trying get mana from performer
        if (!TryComp<BatteryComponent>(args.User, out var playerMana))
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-no-mana-component"), args.User, args.User);
            args.Cancelled = true;
            return;
        }

        if (playerMana.CurrentCharge < requiredMana && _timing.IsFirstTimePredicted)
            Popup.PopupClient(Loc.GetString($"ce-magic-spell-not-enough-mana-cast-warning-{_random.Next(5)}"),
                args.User,
                args.User,
                PopupType.SmallCaution);
    }

    private void OnStaminaCostActionAttempt(Entity<CEActionStaminaCostComponent> ent, ref ActionAttemptEvent args)
    {
        if (!TryComp<StaminaComponent>(args.User, out var staminaComp))
            return;

        if (!staminaComp.Critical)
            return;

        Popup.PopupClient(Loc.GetString("ce-magic-spell-stamina-not-enough"), args.User, args.User);
        args.Cancelled = true;
    }

    private void OnSomaticActionAttempt(Entity<CEActionFreeHandsRequiredComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<HandsComponent>(args.User, out var hands))
        {
            if (_hand.CountFreeHands((args.User, hands)) >= ent.Comp.FreeHandRequired)
                return;
        }

        Popup.PopupClient(Loc.GetString("ce-magic-spell-need-somatic-component"), args.User, args.User);
        args.Cancelled = true;
    }

    private void OnVerbalActionAttempt(Entity<CEActionSpeakingComponent> ent, ref ActionAttemptEvent args)
    {
        if (!HasComp<MutedComponent>(args.User))
            return;

        Popup.PopupClient(Loc.GetString("ce-magic-spell-need-verbal-component"), args.User, args.User);
        args.Cancelled = true;
    }

    private void OnMaterialActionAttempt(Entity<CEActionMaterialCostComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Requirement is null)
            return;

        HashSet<EntityUid> heldedItems = new();

        foreach (var hand in _hand.EnumerateHands(args.User))
        {
            var helded = _hand.GetHeldItem(args.User, hand);
            if (helded is not null)
                heldedItems.Add(helded.Value);
        }

        if (!ent.Comp.Requirement.CheckRequirement(EntityManager, _proto, heldedItems))
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-need-material-component"), args.User, args.User);
            args.Cancelled = true;
        }
    }

    private void OnDangerousActionAttempt(Entity<CEActionDangerousComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<PacifiedComponent>(args.User))
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-pacified"), args.User, args.User);
            args.Cancelled = true;
        }
    }

    private void OnTargetMobStatusRequiredValidate(Entity<CEActionTargetMobStatusRequiredComponent> ent,
        ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        var target = GetEntity(args.Input.EntityTarget);

        if (!TryComp<MobStateComponent>(target, out var mobStateComp))
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-target-not-mob"), args.User, args.User);
            args.Invalid = true;
            return;
        }

        if (!ent.Comp.AllowedStates.Contains(mobStateComp.CurrentState))
        {
            var states = string.Join(", ",
                ent.Comp.AllowedStates.Select(state => state switch
                {
                    MobState.Alive => Loc.GetString("ce-magic-spell-target-mob-state-live"),
                    MobState.Dead => Loc.GetString("ce-magic-spell-target-mob-state-dead"),
                    MobState.Critical => Loc.GetString("ce-magic-spell-target-mob-state-critical")
                }));

            Popup.PopupClient(Loc.GetString("ce-magic-spell-target-mob-state", ("state", states)),
                args.User,
                args.User);
            args.Invalid = true;
        }
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
}
