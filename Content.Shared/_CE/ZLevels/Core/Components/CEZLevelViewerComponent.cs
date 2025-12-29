/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ZLevels.Core.Components;

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

    /// <summary>
    /// Viewed ZLevel relative to entities current ZLevel position.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ZLevelViewRelation Relation;

    [DataField, AutoNetworkedField]
    public int ViewedZLevel;

    [DataField]
    public EntProtoId ActionProto = "CEActionToggleLookUp";

    [DataField, AutoNetworkedField]
    public EntityUid? ZLevelActionEntity;
}

[Flags]
public enum ZLevelViewRelation : byte
{
    Static = 1,  //In Relation to the Worlds Base ZLevel
    Absolute = 2, //In Relation to Grids base ZLevel
    Relative = 4, //In Relation to current Entity ZLevel
}
