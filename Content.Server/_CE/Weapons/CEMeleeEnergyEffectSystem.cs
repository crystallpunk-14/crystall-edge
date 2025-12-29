using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Weapons.MeleeEnergyEffect;
using Content.Shared.Interaction.Events;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server._CE.Weapons;

public sealed class CEMeleeEnergyEffectSystem : CESharedMeleeEnergyEffectSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnChargeChanged(Entity<CEMeleeEnergyEffectComponent> ent, ref ChargeChangedEvent args)
    {
        if (ent.Comp.EnergyRequired > 0 && TryComp<BatteryComponent>(ent, out var battery))
            UpdateBattery(ent, battery);
    }

    private void OnMapInit(Entity<CEMeleeEnergyEffectComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.EnergyRequired > 0 && TryComp<BatteryComponent>(ent, out var battery))
            UpdateBattery(ent, battery);
    }

    protected override void OnUseInHand(Entity<CEMeleeEnergyEffectComponent> ent, ref UseInHandEvent args)
    {
        base.OnUseInHand(ent, ref args);

        if (args.Handled)
            return;

        if (ent.Comp.Active)
            return;

        if (ent.Comp.EnergyRequired > 0 && TryComp<BatteryComponent>(ent, out var battery))
        {
            if (battery.LastCharge < ent.Comp.EnergyRequired)
            {
                Popup.PopupEntity(Loc.GetString("ce-melee-energy-effect-no-energy"), ent.Owner);
                Audio.PlayPvs(ent.Comp.NoEnergySound, ent.Owner);
                return;
            }
            _battery.ChangeCharge((ent, battery), -ent.Comp.EnergyRequired);
            UpdateBattery(ent, battery);
        }

        SetActiveStatus(ent, true, null); //Uhh we dont pass user because its only used for PredictedAudio, and its not works when called from server
    }

    private void UpdateBattery(Entity<CEMeleeEnergyEffectComponent> ent, BatteryComponent battery)
    {
        ent.Comp.Capacity = (int)(battery.MaxCharge / ent.Comp.EnergyRequired);
        ent.Comp.Hits = (int)(battery.LastCharge / ent.Comp.EnergyRequired);
        DirtyField(ent, ent.Comp, nameof(CEMeleeEnergyEffectComponent.Hits));
        DirtyField(ent, ent.Comp, nameof(CEMeleeEnergyEffectComponent.Capacity));
    }
}
