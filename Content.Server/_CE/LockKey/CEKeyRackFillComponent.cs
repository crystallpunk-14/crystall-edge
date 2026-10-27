using Content.Shared._CE.LockKey;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.LockKey;

/// <summary>
/// Once the station is fully loaded, fills this storage with one key for every lock type of the listed groups
/// found in mapped locks of the station. Removed after filling.
/// </summary>
[RegisterComponent, Access(typeof(CEKeyRackFillSystem))]
public sealed partial class CEKeyRackFillComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<CELockGroupPrototype>> Groups = new();

    [DataField]
    public EntProtoId KeyProto = "CEKeyIronBlank";
}
