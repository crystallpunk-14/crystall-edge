using Content.Server._CE.ZLevels.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.ZLevels.Components;

/// <summary>
/// Tracks all Z-level eyes located on other maps
/// </summary>
[RegisterComponent, UnsavedComponent, Access(typeof(CEZLevelsSystem))]
public sealed partial class CEZLevelViewerComponent : Component
{
    public HashSet<EntityUid> Eyes = new();

    [DataField]
    public EntProtoId ActionProto = "CEActionToggleRoofs";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
