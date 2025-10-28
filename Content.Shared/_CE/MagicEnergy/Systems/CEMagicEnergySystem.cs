using Content.Shared._CE.MagicEnergy.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Rounding;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._CE.MagicEnergy.Systems;

public abstract class CESharedMagicEnergySystem : EntitySystem {

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CEEnergyAlertComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEEnergyAlertComponent, ChargeChangedEvent>(OnChargeUpdate);
        SubscribeLocalEvent<CEEnergyOverchargeDamageComponent, CEEnergyOverchargeEvent>(OnOvercharge);
        SubscribeLocalEvent<CEEnergyAlertComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnChargeUpdate(Entity<CEEnergyAlertComponent> ent, ref ChargeChangedEvent args)
    {
        UpdateMagicAlert(ent, null, ent.Comp);
    }

    private void OnStartup(Entity<CEEnergyAlertComponent> ent, ref ComponentStartup args)
    {
        UpdateMagicAlert(ent, null, ent.Comp);
    }

    private void UpdateMagicAlert(EntityUid ent, BatteryComponent? battery = null, CEEnergyAlertComponent? energyAlert = null)
    {
        if (!Resolve(ent, ref battery, false))
            return;
        if (!Resolve(ent, ref energyAlert, false))
            return;

        var level = ContentHelpers.RoundToLevels(
            battery.CurrentCharge,
            battery.MaxCharge,
            _alert.GetMaxSeverity(energyAlert.AlertType));

        _alert.ShowAlert(ent, energyAlert.AlertType, (short) level);
    }

    private void OnOvercharge(Entity<CEEnergyOverchargeDamageComponent> ent, ref CEEnergyOverchargeEvent args)
    {
        _damageable.TryChangeDamage(ent, ent.Comp.Damage * args.Overcharge, interruptsDoAfters: false);
        _jitter.DoJitter(ent, TimeSpan.FromSeconds(0.5f), true, 2, 8);
        _popup.PopupEntity(Loc.GetString(ent.Comp.Popup), ent, PopupType.SmallCaution);

        var xform = Transform(ent);
        SpawnAtPosition(ent.Comp.VFX, xform.Coordinates);
        _audio.PlayPvs(ent.Comp.OverchargeSound, xform.Coordinates);
    }

    private void OnShutdown(Entity<CEEnergyAlertComponent> ent, ref ComponentShutdown args)
    {
        _alert.ClearAlert(ent.Owner, ent.Comp.AlertType);
    }
}

/// <summary>
/// is triggered on entities when the amount of energy received exceeds storage limits
/// </summary>
/// <param name="overcharge">The amount of energy that did not fit into the storage</param>
[ByRefEvent]
public sealed class CEEnergyOverchargeEvent(float overcharge) : EntityEventArgs
{
    public float Overcharge = overcharge;
}
