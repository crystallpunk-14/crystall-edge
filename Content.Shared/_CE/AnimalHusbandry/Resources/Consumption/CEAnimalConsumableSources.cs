using Content.Shared._CE.Consumption;
using Content.Shared.EntityConditions;

namespace Content.Shared._CE.AnimalHusbandry.Resources.Consumption;

/// <summary>
/// Resolves an edible entity stored by a compatible feed trough.
/// </summary>
[DataDefinition]
public sealed partial class CEFeedTroughConsumableSource
    : CEConsumableSourceBase<CEFeedTroughConsumableSource>;

/// <summary>
/// Resolves compatible uncontained food entities in the world.
/// </summary>
[DataDefinition]
public sealed partial class CEWorldFoodConsumableSource
    : CEConsumableSourceBase<CEWorldFoodConsumableSource>;

/// <summary>
/// Resolves compatible uncontained edible drinks in the world.
/// </summary>
[DataDefinition]
public sealed partial class CEWorldDrinkConsumableSource
    : CEConsumableSourceBase<CEWorldDrinkConsumableSource>
{
    /// <summary>
    /// Prototype-authored policy evaluated against the edible solution entity.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition[] Conditions = default!;
}
