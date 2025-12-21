using System.Numerics;
using Content.Shared._CE.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Shared._CE.Power;

public abstract partial class CESharedPowerSystem
{
    private void InitializeGlove()
    {
        SubscribeLocalEvent<CEEnergyTransferGloveComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CEEnergyTransferGloveComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CEEnergyTransferGloveComponent, ExaminedEvent>(OnGloveExamined);
    }

    private void OnAfterInteract(Entity<CEEnergyTransferGloveComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || UseDelay.IsDelayed(ent.Owner))
            return;

        if (args.Target == args.User)
            return;

        var user = args.User;
        var target = args.Target.Value;

        UseDelay.TryResetDelay(ent);
        _audio.PlayPvs(ent.Comp.UseSound, ent);

        if (!BatteryQuery.TryComp(user, out var userBattery))
        {
            _popup.PopupEntity(Loc.GetString("ce-energy-transfer-glove-cant-use"), ent, args.User);
            return;
        }

        BatteryQuery.TryComp(target, out var batteryTarget);
        SpawnAtPosition(ent.Comp.VFX, Transform(args.Target.Value).Coordinates);

        if (ent.Comp.ConsumeMode)
        {
            if (batteryTarget is null)
                return;

            var drained = -Battery.ChangeCharge((target, batteryTarget), -ent.Comp.TransferAmount);
            if (drained <= 0)
                return;

            Battery.ChangeCharge((user, userBattery), drained);
            args.Handled = true;
        }
        else
        {
            var spent = -Battery.ChangeCharge((user, userBattery), -ent.Comp.TransferAmount);

            if (batteryTarget is null)
                return;

            if (spent <= 0)
                return;

            Battery.ChangeCharge((target, batteryTarget), spent);
            args.Handled = true;
        }
    }

    private void OnGloveExamined(Entity<CEEnergyTransferGloveComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-energy-transfer-glove-examine",
            ("mode",
                Loc.GetString(ent.Comp.ConsumeMode
                    ? "ce-energy-transfer-glove-mode-drain"
                    : "ce-energy-transfer-glove-mode-transfer"))));
    }

    private void OnUseInHand(Entity<CEEnergyTransferGloveComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || UseDelay.IsDelayed(ent.Owner))
            return;

        args.Handled = true;

        UseDelay.TryResetDelay(ent);

        ent.Comp.ConsumeMode = !ent.Comp.ConsumeMode;
        Dirty(ent);

        _audio.PlayPvs(ent.Comp.ConsumeMode ? ent.Comp.ConsumeModeSound : ent.Comp.TransferModeSound, ent);
    }
}
