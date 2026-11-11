using Content.Client._CE.HoverBar;
using Content.Client.Overlays;
using Content.Shared._CE.MedicalVision;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Client._CE.MedicalVision;

/// <summary>
/// While the local player has medical vision, adds health and stamina bars above the hovered creature
/// </summary>
public sealed partial class CEMedicalVisionHudSystem : EquipmentHudSystem<CEMedicalVisionComponent>
{
    [Dependency] private CEHoverBarSystem _hoverBar = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    private const string HealthBar = "Health";
    private const string StaminaBar = "Stamina";
    private const int HealthOrder = 0;
    private const int StaminaOrder = 1;

    private static readonly Color HealthColor = Color.FromHex("#c8423c");
    private static readonly Color CriticalColor = Color.FromHex("#5e1a1a");
    private static readonly Color StaminaColor = Color.FromHex("#4caf50");

    protected override void UpdateInternal(RefreshEquipmentHudEvent<CEMedicalVisionComponent> args)
    {
        base.UpdateInternal(args);
        _hoverBar.SetSourceActive(this, true);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _hoverBar.SetSourceActive(this, false);
    }

    /// <summary>
    /// Alive: the main fill is health left until crit over a full dark red layer.
    /// In crit the dark red layer shrinks: it is the damage left until death.
    /// Mobs without a crit threshold get a single layer down to death.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollectHealthBar(Entity<MobThresholdsComponent> ent, ref CECollectHoverBarsEvent args)
    {
        if (!IsActive || !TryComp<DamageableComponent>(ent, out var damageable))
            return;

        if (!_thresholds.TryGetThresholdForState(ent, MobState.Dead, out var dead, ent.Comp))
            return;

        var deadValue = dead.Value.Float();
        if (deadValue <= 0f)
            return;

        var damage = _damageable.GetTotalDamage((ent, damageable)).Float();

        if (!_thresholds.TryGetThresholdForState(ent, MobState.Critical, out var crit, ent.Comp)
            || crit.Value <= 0
            || crit.Value >= dead.Value)
        {
            args.Bars.Add(new CEHoverBar(HealthBar, HealthOrder, Math.Clamp(1f - damage / deadValue, 0f, 1f), HealthColor));
            return;
        }

        var critValue = crit.Value.Float();
        var fill = Math.Clamp(1f - damage / critValue, 0f, 1f);
        var secondary = Math.Clamp(1f - (damage - critValue) / (deadValue - critValue), 0f, 1f);

        args.Bars.Add(new CEHoverBar(HealthBar, HealthOrder, fill, HealthColor, secondary, CriticalColor));
    }

    [SubscribeLocalEvent]
    private void OnCollectStaminaBar(Entity<StaminaComponent> ent, ref CECollectHoverBarsEvent args)
    {
        if (!IsActive || ent.Comp.CritThreshold <= 0f)
            return;

        var damage = _stamina.GetStaminaDamage(ent, ent.Comp);
        var fill = Math.Clamp((ent.Comp.CritThreshold - damage) / ent.Comp.CritThreshold, 0f, 1f);

        args.Bars.Add(new CEHoverBar(StaminaBar, StaminaOrder, fill, StaminaColor));
    }
}
