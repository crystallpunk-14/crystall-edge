using System.Numerics;
using Content.Server._CE.Power.Components;
using Content.Shared._CE.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Robust.Server.Audio;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem
{
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private void InitializeGlove()
    {
        SubscribeLocalEvent<CEEnergyTransferGloveComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CEEnergyTransferGloveComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CEEnergyTransferGloveComponent, ExaminedEvent>(OnGloveExamined);
    }

    private void OnAfterInteract(Entity<CEEnergyTransferGloveComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || _useDelay.IsDelayed(ent.Owner))
            return;

        if (args.Target == args.User)
            return;

        var user = args.User;
        var target = args.Target.Value;

        _useDelay.TryResetDelay(ent);
        _audio.PlayPvs(ent.Comp.UseSound, ent);

        if (!_batteryQuery.TryComp(user, out var userBattery))
        {
            _popup.PopupEntity(Loc.GetString("ce-energy-transfer-glove-cant-use"), ent, args.User);
            return;
        }

        _batteryQuery.TryComp(target, out var batteryTarget);
        SpawnAtPosition(ent.Comp.VFX, Transform(args.Target.Value).Coordinates);

        if (ent.Comp.ConsumeMode)
        {
            if (batteryTarget is null)
                return;

            var drained = -_battery.ChangeCharge((target, batteryTarget), -ent.Comp.TransferAmount);
            if (drained <= 0)
                return;

            var drainedPercent = drained / ent.Comp.TransferAmount;

            _battery.ChangeCharge((user, userBattery), drained);
            PullTowardsUser(target, user, ent.Comp.PullDistance * drainedPercent, ent.Comp.ThrowPower);
            args.Handled = true;
        }
        else
        {
            var spent = -_battery.ChangeCharge((user, userBattery), -ent.Comp.TransferAmount);
            PushFromUser(target, user, ent.Comp.ThrowDistance, ent.Comp.ThrowPower);

            if (batteryTarget is null)
                return;

            if (spent <= 0)
                return;

            _battery.ChangeCharge((target, batteryTarget), spent);
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

    private void PushFromUser(EntityUid target, EntityUid user, float distance, float power)
    {
        var dir = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(user);
        if (dir == Vector2.Zero)
            return;

        var displacement = Vector2.Normalize(dir) * distance;
        _throw.TryThrow(target, displacement, power, user, doSpin: true);
    }

    private void PullTowardsUser(EntityUid target, EntityUid user, float distance, float power)
    {
        var dir = _transform.GetWorldPosition(user) - _transform.GetWorldPosition(target);
        if (dir == Vector2.Zero)
            return;

        var displacement = Vector2.Normalize(dir) * distance;
        _throw.TryThrow(target, displacement, power, user, doSpin: true);
    }

    private void OnUseInHand(Entity<CEEnergyTransferGloveComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || _useDelay.IsDelayed(ent.Owner))
            return;

        args.Handled = true;

        _useDelay.TryResetDelay(ent);

        ent.Comp.ConsumeMode = !ent.Comp.ConsumeMode;
        Dirty(ent);

        _audio.PlayPvs(ent.Comp.ConsumeMode ? ent.Comp.ConsumeModeSound : ent.Comp.TransferModeSound, ent);
    }
}
