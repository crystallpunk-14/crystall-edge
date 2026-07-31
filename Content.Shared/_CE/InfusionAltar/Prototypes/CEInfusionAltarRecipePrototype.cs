using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.ResourceManager;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.InfusionAltar.Prototypes;

[Prototype("infusionAltarRecipe")]
public sealed partial class CEInfusionAltarRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The item that must be placed on the altar's central pedestal to identify this recipe.
    /// Consumed on completion, together with everything else the recipe requires.
    /// </summary>
    [DataField(required: true)]
    public CEResourceRequirement Catalyst = default!;

    /// <summary>
    /// Relative weights used to distribute this recipe's rolled essence amount (<see cref="EssenceAmount"/>)
    /// across essence types. Rolled once per round; see <see cref="Content.Server._CE.InfusionAltar.CEInfusionAltarSystem"/>.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> EssenceWeights = new();

    /// <summary>
    /// Total amount of essence points (summed across all types) this recipe requires. Rolled once per
    /// round, then distributed across <see cref="EssenceWeights"/> by weighted random.
    /// </summary>
    [DataField(required: true)]
    public MinMax EssenceAmount;

    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public int ResultCount = 1;
}