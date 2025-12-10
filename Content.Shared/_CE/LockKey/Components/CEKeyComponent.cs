using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.LockKey.Components;

/// <summary>
/// a key component that can be used to unlock and lock locks from CPLockComponent
/// </summary>
[RegisterComponent, AutoGenerateComponentState(fieldDeltas: true), NetworkedComponent, Access(typeof(CESharedLockKeySystem))]
public sealed partial class CEKeyComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<int> Shape = new();

    /// <summary>
    /// If not null, automatically generates a key for the specified category on initialization. This ensures that the lock will be opened with a key of the same category.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CELockTypePrototype>? AutoGenerateShape = null;
}
