using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ThirdArm.Components;

[RegisterComponent]
public sealed partial class CEThirdArmModuleActionsGrantComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Actions = new();
}