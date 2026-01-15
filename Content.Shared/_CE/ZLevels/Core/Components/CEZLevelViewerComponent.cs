/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Alert;
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
    /// Viewed ZLevel relative to entities current ZLevel position.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ZLevelViewRelation Relation;

    [DataField, AutoNetworkedField]
    public int ViewedZLevel;

    [DataField]
    public ProtoId<AlertPrototype> ZLayerAlert = "CEZLayer";
    [DataField]
    public CEZLayerAlertSeverity ZLayerAlertSeverity = CEZLayerAlertSeverity.neutral;

}

public enum ZLevelViewRelation : byte
{
    Static = 1,  //In Relation to the Worlds Base ZLevel
    Absolute = 2, //In Relation to Grids base ZLevel
    Relative = 3, //In Relation to current Entity ZLevel
}
