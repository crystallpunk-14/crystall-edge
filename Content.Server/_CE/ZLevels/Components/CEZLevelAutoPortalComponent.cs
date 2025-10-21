using Content.Server._CE.ZLevels.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.ZLevels.Components;

/// <summary>
/// automatically creates a linked portal at a different relative z-level, and then the component is removed
/// </summary>
[RegisterComponent, Access(typeof(CEStationZLevelsSystem))]
public sealed partial class CEZLevelAutoPortalComponent : Component
{
    /// <summary>
    /// relative neighboring layer. Ideally, -1 is the neighboring bottom layer, +1 is the neighboring top layer
    /// </summary>
    [DataField(required: true)]
    public int ZLevelOffset = 0;

    /// <summary>
    /// prototype of the portal being created on the other side
    /// </summary>
    [DataField(required: true)]
    public EntProtoId OtherSideProto = default!;
}
