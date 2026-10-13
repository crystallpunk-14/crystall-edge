/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Cooking.Components;

/// <summary>
/// Prepares food from ingredients and places it in the <see cref="CEFoodHolderComponent"/> on the same entity.
/// Food type and solution are taken from that holder.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CESharedCookingSystem))]
public sealed partial class CEFoodCookerComponent : Component
{
    [DataField(required: true)]
    public string ContainerId = string.Empty;

    public DoAfterId? DoAfterId = null;

    /// <summary>
    /// The moment in time when this entity was last heated. Used for calculations when DoAfter cooking should be reset.
    /// </summary>
    [DataField]
    public TimeSpan LastHeatingTime = TimeSpan.Zero;

    /// <summary>
    /// How often do you need to heat this entity so that doAfter does not reset?
    /// </summary>
    /// <remarks>
    /// Ideally, this system should check that the Cooker temperature is within certain limits,
    /// but at the moment the entire temperature system is a terrible mess that we don't want to get deeply involved with,
    /// so we simplify the simulation by simply requiring the heating action to be performed.
    /// We don't care how much it heats up, we care how long it takes.
    /// </remarks>
    [DataField]
    public TimeSpan HeatingFrequencyRequired = TimeSpan.FromSeconds(2f);
}

/// <summary>
/// Everything about a dish that reagents cannot store. Copy it with <c>ISerializationManager.CreateCopy</c>:
/// the visual layers are reference types, so a shallow copy would share them with the recipe prototype.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class CEFoodData
{
    [DataField]
    public ProtoId<CECookingRecipePrototype>? CurrentRecipe;

    [DataField]
    public LocId? Name;

    [DataField]
    public LocId? Desc;

    [DataField]
    public List<PrototypeLayerData> Visuals = new();

    /// <summary>
    /// Trash left from the ingredients. Dropped next to the food holder once its food runs out.
    /// </summary>
    [DataField]
    public List<EntProtoId> Trash = new();

    [DataField]
    public HashSet<ProtoId<FlavorPrototype>> Flavors = new();

    /// <summary>
    /// Status effects granted by consuming food with this FoodData. Effects stack, in seconds, and are multiplied by the number of units consumed.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, float> StatusEffects = new()
    {
        {"CEStatusEffectGoodFoodRegen", 2f }
    };
}

[Serializable, NetSerializable]
public enum CECookingVisuals : byte
{
    Cooking,
    Burning,
}
