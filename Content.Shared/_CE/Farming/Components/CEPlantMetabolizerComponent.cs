using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// allows the plant to obtain resources by absorbing liquid from the ground
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantMetabolizerComponent : Component
{
    [DataField]
    public FixedPoint2 SolutionPerUpdate = 5f;

    /// <summary>
    /// how reagents affect plants. It is important to note that the effect specified in the metabolism will only work 100%
    /// if the specified reagent accounted for 100% of the solution “consumed” during renewal.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, List<CEMetabolizerEffect>> Metabolization = new();

    /// <summary>
    /// Solution for metabolizing resources
    /// </summary>
    [DataField]
    public string Solution = "plant";
}

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEMetabolizerEffect
{
    public abstract void Effect(Entity<CEPlantComponent> plant, FixedPoint2 amount, EntityManager entityManager);
}

