using Content.Shared._CE.Actions.Components;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.SSDIndicator;
using Robust.Shared.Analyzers;
using Robust.Shared.Map;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    /// <summary>
    /// Before using a spell, a mana check is made for the amount of mana to show warnings.
    /// </summary>
    [SubscribeLocalEvent]
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

        if (_battery.GetCharge((args.User, playerMana)) < requiredMana)
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-not-enough-mana"), args.User, args.User);
            args.Cancelled = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnEssenceCostActionAttempt(Entity<CEActionEssenceCostComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_magicFocus.HasEnoughEssence(args.User, ent.Comp.EssenceCost))
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-not-enough-essence"), args.User, args.User);
            args.Cancelled = true;
        }
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnActionSSDAttempt(Entity<CEActionSSDBlockComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid || args.TargetInvalid || args.Input.EntityTarget is not { } netTarget)
            return;

        if (!TryGetEntity(netTarget, out var resolvedTarget) ||
            resolvedTarget is not { } target || TerminatingOrDeleted(target))
        {
            args.TargetInvalid = true;
            return;
        }

        if (!TryComp<SSDIndicatorComponent>(target, out var ssdIndication))
            return;

        if (ssdIndication.IsSSD)
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-ssd"), args.User, args.User);
            args.TargetInvalid = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnLineOfSightValidate(Entity<CEActionRequireLineOfSightComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid || args.TargetInvalid)
            return;

        EntityCoordinates? target = null;
        if (args.Input.EntityCoordinatesTarget is { } netCoords)
        {
            if (!float.IsFinite(netCoords.X) || !float.IsFinite(netCoords.Y) ||
                !TryGetEntity(netCoords.NetEntity, out var resolvedParent) ||
                resolvedParent is not { } parent || TerminatingOrDeleted(parent) ||
                !HasComp<TransformComponent>(parent))
            {
                args.TargetInvalid = true;
                return;
            }

            target = new EntityCoordinates(parent, netCoords.Position);
        }
        else if (args.Input.EntityTarget is { } netEntity)
        {
            if (!TryGetEntity(netEntity, out var resolvedTarget) ||
                resolvedTarget is not { } entity || TerminatingOrDeleted(entity) ||
                !TryComp(entity, out TransformComponent? transform))
            {
                args.TargetInvalid = true;
                return;
            }

            target = transform.Coordinates;
        }

        if (target is not { } coords)
            return;

        var range = TryComp<TargetActionComponent>(ent, out var targetAction) ? targetAction.Range : 0f;

        // Raycasts the occluder tree (the same OccluderComponent data that drives client FOV/lighting),
        // not physics fixtures — so opaque walls block the action while transparent windows do not.
        if (_examine.InRangeUnOccluded(args.User, coords, range))
            return;

        Popup.PopupClient(Loc.GetString("dash-ability-cant-see"), args.User, args.User);
        args.TargetInvalid = true;
    }

    [SubscribeLocalEvent]
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
