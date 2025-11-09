using Content.Shared._CE.Farming.Prototypes;
using Content.Shared.FixedPoint;
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

    [DataField(required: true)]
    public ProtoId<CEPlantMetabolizerPrototype> MetabolizerId;
}
