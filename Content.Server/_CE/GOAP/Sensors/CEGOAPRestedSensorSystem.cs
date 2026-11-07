using Content.Server._CE.MeleeWeapon;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared._CE.MeleeWeapon.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._CE.GOAP.Sensors;

public sealed partial class CEGOAPRestedSensorEntry
    : CEGOAPSensorEntry<CEGOAPRestedSensorEntry, CEGOAPRestedSensorComponent>
{
    /// <summary>
    /// Weapon use type whose stamina cost decides whether the agent can afford one more swing.
    /// </summary>
    [DataField]
    public CEUseType UseType = CEUseType.Primary;

    /// <summary>
    /// Remaining stamina fraction (0..1) the tired agent has to recover above before it counts as rested again.
    /// </summary>
    [DataField]
    public float RecoverThreshold = 0.5f;
}

/// <summary>
/// True while the agent has the stamina to fight. Goes false when one more swing would stamina-crit
/// the agent and stays false until stamina recovers above <see cref="CEGOAPRestedSensorEntry.RecoverThreshold"/>.
/// Agents without stamina are always rested. Event-driven via <see cref="CEStaminaChangedEvent"/> on the agent.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPRestedSensorComponent : CEGOAPSensorComponent<CEGOAPRestedSensorEntry>;

public sealed partial class CEGOAPRestedSensorSystem
    : CEGOAPSensorSystem<CEGOAPRestedSensorComponent, CEGOAPRestedSensorEntry>
{
    [Dependency] private CEWeaponSystem _weapon = default!;

    [Dependency] private EntityQuery<CEGOAPComponent> _goapQuery = default!;
    [Dependency] private EntityQuery<StaminaComponent> _staminaQuery = default!;
    [Dependency] private EntityQuery<CEWeaponStaminaCostComponent> _costQuery = default!;

    [SubscribeLocalEvent]
    private void OnStaminaChanged(Entity<CEGOAPRestedSensorComponent> ent, ref CEStaminaChangedEvent args)
    {
        EvaluateAll(ent);
    }

    protected override bool Evaluate(EntityUid agent, CEGOAPRestedSensorEntry entry, CEGOAPSelectorResult target)
    {
        if (!_staminaQuery.TryComp(agent, out var stamina) || !_goapQuery.TryComp(agent, out var goap))
            return true;

        var wasRested = goap.WorldState.GetValueOrDefault(entry.ConditionKey);
        var rested = IsRested((agent, stamina), entry, wasRested);

        // Drop the running attack right away instead of waiting for the plan cooldown.
        if (rested != wasRested)
            goap.NextPlanTime = TimeSpan.Zero;

        return rested;
    }

    private bool IsRested(Entity<StaminaComponent> agent, CEGOAPRestedSensorEntry entry, bool wasRested)
    {
        if (agent.Comp.Critical || agent.Comp.CritThreshold <= 0f)
            return false;

        var damage = agent.Comp.StaminaDamage;

        if (!wasRested)
            return 1f - damage / agent.Comp.CritThreshold > entry.RecoverThreshold;

        return damage + GetSwingCost(agent, entry.UseType) < agent.Comp.CritThreshold;
    }

    private float GetSwingCost(EntityUid agent, CEUseType useType)
    {
        if (!_weapon.TryGetWeapon(agent, out var weapon) || !_costQuery.TryComp(weapon.Value, out var cost))
            return 0f;

        return cost.Costs.GetValueOrDefault(useType);
    }
}
