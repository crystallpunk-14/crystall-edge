using System.Numerics;
using Content.Server._CE.Power.Components;
using Content.Shared._CE.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Content.Shared.Timing;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem
{
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

        if (!_batteryQuery.TryComp(user, out var userBattery))
        {
            //popup todo
            return;
        }

        _useDelay.TryResetDelay(ent);
        _batteryQuery.TryComp(target, out var batteryTarget);

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
            ("mode", ent.Comp.ConsumeMode
                ? Loc.GetString("ce-energy-transfer-glove-examine-drain")
                : Loc.GetString("ce-energy-transfer-glove-examine-transfer"))));
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
        if (args.Handled)
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        _useDelay.TryResetDelay(ent);

        ent.Comp.ConsumeMode = !ent.Comp.ConsumeMode;
    }
}
