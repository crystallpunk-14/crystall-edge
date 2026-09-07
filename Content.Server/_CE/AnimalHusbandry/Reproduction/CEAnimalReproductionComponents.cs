using Content.Shared.Whitelist;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Reproduction;

/// <summary>
/// Marks a physical product that can be placed in an incubation host.
/// Fertilized products compose standard trigger components in their prototype.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalIncubationComponent : Component
{
    [DataField]
    public bool Fertilized;
}

/// <summary>
/// Marks a fixed-slot host as an incubation location for physical animal products.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalIncubationHostComponent : Component
{
    [DataField(required: true)]
    public LocId ExamineMessage;
}

/// <summary>
/// Mating grants a limited number of fertilized products. A failed placement must not spend them.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalFertilityComponent : Component
{
    [DataField] public int ProductsRemaining;
}

/// <summary>
/// Selects a fertilized product when the producer has fertility and the authored population limit permits it.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalFertilizableProductComponent : Component
{
    [DataField(required: true)]
    public EntProtoId UnfertilizedPrototype;

    [DataField(required: true)]
    public EntProtoId FertilizedPrototype;

    [DataField(required: true)]
    public EntityWhitelist PopulationWhitelist = default!;

    [DataField(required: true)]
    public int PopulationLimit;
}
