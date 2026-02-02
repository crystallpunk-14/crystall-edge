using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.Pathfinding;

[RegisterComponent]
public sealed partial class CEZLevelNavBridgeComponent : Component
{
    [DataField]
    public MapId? TargetMap;

    [DataField]
    public EntityUid? TargetEntity;

    [DataField]
    public Vector2 TransitionPoint = Vector2.Zero;

    [ViewVariables]
    public readonly Dictionary<EntityCoordinates, int> PortalHandels = new();


}
