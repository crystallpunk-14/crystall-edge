using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RadialConstruction;

/// <summary>
/// Component that allows entities to be crafted/constructed from a radial menu when interacted with using
/// an item that satisfies one of its <see cref="Variants"/>' triggers (a tool quality, a material stack, ...).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CERadialConstructionComponent : Component
{
    [DataField(required: true)]
    public List<CERadialConstructionVariant> Variants = new();

    [DataField]
    public float Delay = 1;
}

/// <summary>
/// One entry of a <see cref="CERadialConstructionComponent"/>: the item interaction that opens this variant's
/// menu, and the prototypes offered in it.
/// </summary>
[DataDefinition]
public sealed partial class CERadialConstructionVariant
{
    [DataField(required: true)]
    public CERadialConstructionTrigger Trigger = default!;

    [DataField(required: true)]
    public List<EntProtoId> AvailablePrototypes = new();
}
