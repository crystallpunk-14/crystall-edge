using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Allows entity to see through Z-levels
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), UnsavedComponent, Access(typeof(CESharedZLevelsSystem))]
public sealed partial class CEZLevelViewerComponent : Component
{
    public HashSet<EntityUid> Eyes = new();

    /// <summary>
    /// We can look at 1 z-level up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LookUp;

    [DataField]
    public EntProtoId ActionProto = "CEActionToggleLookUp";

    [DataField, AutoNetworkedField]
    public EntityUid? ZLevelActionEntity;

    [DataField, AutoNetworkedField]
    public float ThrowUpForce = 5f; //I dont really like this in viewer component
}
