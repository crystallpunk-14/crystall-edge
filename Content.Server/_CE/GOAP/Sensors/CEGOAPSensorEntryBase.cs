using Content.Shared._CE.GOAP.Selectors;
using Content.Shared._CE.GOAP.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public abstract partial class CEGOAPSensorEntryBase
{
    [DataField(required: true)]
    public ProtoId<CEGOAPConditionPrototype> ConditionKey;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;
}
