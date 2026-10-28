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

    /// <summary>
    /// Specific entities tracked by this owner, each with the waypointer prototype it is drawn with.
    /// Only resolved on the server, since their sources (e.g. objective targets) aren't networked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, ProtoId<CEWaypointerPrototype>> TrackedEntities = new();

    /// <summary>
    /// Server-only: entities currently PVS-overridden to the owner's session for <see cref="TrackedEntities"/>.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> PvsOverrides = new();

    public override bool SendOnlyToOwner => true;
}
