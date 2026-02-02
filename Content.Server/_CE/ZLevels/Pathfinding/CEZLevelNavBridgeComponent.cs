using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.Pathfinding;

[RegisterComponent]
public sealed partial class CEZLevelNavBridgeComponent : Component
{
    [ViewVariables]
    public MapId? TargetMap = null;

    [ViewVariables]
    public EntityUid? TargetEntity = null;

    [DataField]
    public Vector2 TransitionPoint = Vector2.Zero;

    [ViewVariables]
    public readonly Dictionary<EntityCoordinates, int> PortalHandels = new();


}
