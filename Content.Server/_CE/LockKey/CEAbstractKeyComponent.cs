using Content.Shared._CE.LockKey;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.LockKey;

[RegisterComponent]
public sealed partial class CEAbstractKeyComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CELockGroupPrototype> Group = default;

    [DataField]
    public bool DeleteOnFailure = true;
}
