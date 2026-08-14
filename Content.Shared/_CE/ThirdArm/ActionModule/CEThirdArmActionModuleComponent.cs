using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ThirdArm.ActionModule;

[RegisterComponent]
public sealed partial class CEThirdArmActionModuleComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Actions = new();
}
