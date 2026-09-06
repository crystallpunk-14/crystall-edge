using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Shared._CE.Consumption;

/// <summary>
/// Selects a non-edible drainable solution whose resolved solution entity
/// passes prototype-authored conditions.
/// </summary>
[DataDefinition]
public sealed partial class CEDrainableSolutionConsumableSource
    : CEConsumableSourceBase<CEDrainableSolutionConsumableSource>
{
    [DataField]
    public EntityWhitelist? ProviderWhitelist;

    [DataField]
    public EntityWhitelist? ProviderBlacklist;

    [DataField(required: true)]
    public EntityCondition[] Conditions = default!;

    [DataField(required: true)]
    public FixedPoint2 TransferAmount;
}
