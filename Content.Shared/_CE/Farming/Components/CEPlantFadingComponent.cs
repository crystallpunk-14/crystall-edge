using Content.Shared.Damage;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Damages the plant on every plant update while its <see cref="CEPlantComponent.Resource"/> is fully depleted.
/// The plant's death is left to <c>Destructible</c> thresholds on the prototype, so the lethal amount is tuned there.
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantFadingComponent : Component
{
    /// <summary>
    /// Damage dealt on each plant update when the plant has run out of resource.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();
}
