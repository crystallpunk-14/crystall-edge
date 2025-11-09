using Content.Shared._CE.Farming.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Prototypes;

/// <summary>
/// Allows the plant to drink chemicals. The effect of the drank reagents depends on the selected metabolizer.
/// </summary>
[Prototype("CEPlantMetabolizer")]
public sealed partial class CEPlantMetabolizerPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, List<CEMetabolizerEffect>> Metabolization = new();
}

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEMetabolizerEffect
{
    public abstract void Effect(Entity<CEPlantComponent> plant, FixedPoint2 amount, EntityManager entityManager);
}
