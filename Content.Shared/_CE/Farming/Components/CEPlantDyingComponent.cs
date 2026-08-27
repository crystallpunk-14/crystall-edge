using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Kills the plant once it has taken enough withering damage, optionally leaving remains behind.
/// Reacts to damage directly so plants don't each have to copy a whole <c>Destructible</c> threshold.
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantDyingComponent : Component
{
    /// <summary>
    /// Damage group that counts as withering. <see cref="CEPlantFadingComponent"/> deals Cellular, which is in Genetic.
    /// </summary>
    [DataField]
    public ProtoId<DamageGroupPrototype> DamageGroup = "Genetic";

    /// <summary>
    /// The plant dies when its accumulated positive damage in <see cref="DamageGroup"/> reaches this value.
    /// </summary>
    [DataField]
    public float DeathThreshold = 10f;

    /// <summary>
    /// Entity spawned in the plant's tile when it dies of withering. Null - the plant just disappears.
    /// Spawned anchored, so it blocks planting a new seed there until cleared.
    /// </summary>
    [DataField]
    public EntProtoId? DeadEntity;
}
