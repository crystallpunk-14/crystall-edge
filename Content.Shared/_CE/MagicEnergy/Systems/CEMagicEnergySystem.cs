using Content.Shared._CE.MagicEnergy.Components;
using Content.Shared.Damage;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._CE.MagicEnergy.Systems;

public abstract class CESharedMagicEnergySystem : EntitySystem {

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CEEnergyOverchargeDamageComponent, CEEnergyOverchargeEvent>(OnOvercharge);
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
