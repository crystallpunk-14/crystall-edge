using Robust.Shared.Prototypes;

namespace Content.Shared._CE.GOAP.Prototypes;

/// <summary>
/// A declared GOAP world state key. Goals, actions, sensors and external writers reference keys
/// through <see cref="ProtoId{T}"/>, so a typo in YAML fails prototype validation instead of
/// silently creating a key nobody produces.
/// </summary>
[Prototype("GOAPCondition")]
public sealed partial class CEGOAPConditionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
