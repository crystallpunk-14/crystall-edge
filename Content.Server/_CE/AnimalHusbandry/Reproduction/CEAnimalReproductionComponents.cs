using Content.Shared.EntityConditions;
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
/// Optionally replaces a produced prototype when prototype-authored population,
/// mate whitelist and mate conditions permit fertilization.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalFertilizableProductComponent : Component
{
    [DataField(required: true)]
    public EntProtoId UnfertilizedPrototype;

    [DataField(required: true)]
    public EntProtoId FertilizedPrototype;

    [DataField(required: true)]
    public float FertilizationRange;

    /// <summary>
    /// Null means that no nearby mate is required.
    /// </summary>
    [DataField]
    public EntityWhitelist? MateWhitelist;

    [DataField, AlwaysPushInheritance]
    public EntityCondition[] MateConditions = Array.Empty<EntityCondition>();

    [DataField(required: true)]
    public EntityWhitelist PopulationWhitelist = default!;

    [DataField(required: true)]
    public int PopulationLimit;
}
