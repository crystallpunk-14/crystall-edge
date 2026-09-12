using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Waypointer;

/// <summary>
/// This signifies an entity with an active waypointer trying to track something.
/// This is NOT a pinpointer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEWaypointerComponent : Component
{
    /// <summary>
    /// The resolved set of waypointer prototypes currently visible for the owner of this component.
    /// Recomputed by CESharedWaypointerSystem.RefreshWaypointers from every active source (clothing, statuses, etc).
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CEWaypointerPrototype>> WaypointerProtoIds = new();

    public override bool SendOnlyToOwner => true;
}
