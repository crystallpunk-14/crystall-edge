using Content.Shared.Damage;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Spends the plant's energy and resource to mend accumulated damage.
/// Runs before growth and fruit production, so a wounded plant recovers before it spends anything on growing.
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantHealingComponent : Component
{
    /// <summary>
    /// Energy spent per plant update while healing.
    /// </summary>
    [DataField]
    public float EnergyCost = 1f;

    /// <summary>
    /// Resource spent per plant update while healing.
    /// </summary>
    [DataField]
    public float ResourceCost = 1f;

    /// <summary>
    /// Damage mended on each plant update, provided the plant can pay the cost and has matching damage to heal.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Heal = new();
}
