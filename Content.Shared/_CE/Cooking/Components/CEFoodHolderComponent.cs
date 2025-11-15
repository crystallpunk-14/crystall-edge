/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Components;

/// <summary>
/// Food of the specified type can be transferred to this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true), Access(typeof(CESharedCookingSystem))]
public sealed partial class CEFoodHolderComponent : Component
{
    /// <summary>
    /// What food is currently stored here?
    /// </summary>
    [DataField, AutoNetworkedField]
    public CEFoodData? FoodData;

    [DataField]
    public bool CanAcceptFood;

    [DataField]
    public bool CanGiveFood;

    [DataField(required: true)]
    public ProtoId<CEFoodTypePrototype> FoodType;

    [DataField]
    public string? SolutionId;

    [DataField]
    public int MaxDisplacementFillLevels = 8;

    [DataField]
    public string? DisplacementRsiPath;

    /// <summary>
    /// target layer, where new layers will be added. This allows you to control the order of generative layers and static layers.
    /// </summary>
    [DataField]
    public string TargetLayerMap = "ce_foodLayers";

    public HashSet<string> RevealedLayers = new();
}
