using Robust.Shared.Prototypes;

namespace Content.Shared._CE.GOAP.Prototypes;

/// <summary>
/// A declared GOAP target slot. The slot's selector is defined once per mob (inline or by a
/// <see cref="CEGOAPBehaviorPrototype"/>) and actions and sensors reference it by id.
/// </summary>
[Prototype("GOAPTarget")]
public sealed partial class CEGOAPTargetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
