using Content.Shared._CE.GOAP.Selectors;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public abstract partial class CEGOAPSensorEntryBase
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;
}
