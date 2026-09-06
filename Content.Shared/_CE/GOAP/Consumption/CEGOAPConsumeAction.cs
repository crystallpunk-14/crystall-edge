namespace Content.Shared._CE.GOAP.Consumption;

/// <summary>
/// Consumes from a consumable-provider selector through its typed source and the
/// canonical ingestion system. Movement remains a separate GOAP action.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPConsumeAction : CEGOAPActionBase<CEGOAPConsumeAction>
{
    [DataField(required: true)]
    public TimeSpan RetryDelay;
}
