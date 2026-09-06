using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Lifecycle;

/// <summary>
/// Replaces an entity with one prototype after a configured number of nights
/// on which all standard entity conditions pass.
/// </summary>
[RegisterComponent]
public sealed partial class CEConditionalNightGrowthComponent : Component
{
    [DataField(required: true)]
    public int RequiredSuccessfulNights;

    [DataField(required: true)]
    public EntProtoId ResultPrototype;

    [DataField, AlwaysPushInheritance]
    public EntityCondition[] Conditions = Array.Empty<EntityCondition>();

    [DataField, ViewVariables]
    public int SuccessfulNights;
}

/// <summary>
/// Lets canonical state owners copy their state into a permanent replacement
/// without coupling the generic transformation to storage components.
/// </summary>
[ByRefEvent]
public record struct CEEntityReplacementStateTransferEvent(EntityUid Replacement)
{
    public bool Cancelled;
}
