using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Lifecycle;

/// <summary>
/// Remaining healthy simulation time before this animal becomes its adult prototype.
/// Entity queries exclude paused entities; this duration needs no calendar or pause offset.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalGrowthComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Remaining = TimeSpan.FromMinutes(36);

    /// <summary>A refused container replacement is retried without repeatedly spawning every frame.</summary>
    [DataField]
    public TimeSpan RetryRemaining;

    [DataField(required: true)]
    public EntProtoId ResultPrototype;

    [DataField, AlwaysPushInheritance]
    public EntityCondition[] Conditions = [];
}
