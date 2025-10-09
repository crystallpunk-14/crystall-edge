using Robust.Shared.Prototypes;

namespace Content.Shared._CE.LockKey.Components;

/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class CEStationKeyDistributionComponent : Component
{
    [DataField]
    public List<ProtoId<CELockTypePrototype>> Keys = new();
}
