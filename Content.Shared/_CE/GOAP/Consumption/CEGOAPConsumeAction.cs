namespace Content.Shared._CE.GOAP.Consumption;

/// <summary>
/// Consumes the selected edible through the canonical ingestion system.
/// Movement remains a separate GOAP action.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPConsumeAction : CEGOAPActionBase<CEGOAPConsumeAction>
{
    [DataField(required: true)]
    public TimeSpan RetryDelay;
}
