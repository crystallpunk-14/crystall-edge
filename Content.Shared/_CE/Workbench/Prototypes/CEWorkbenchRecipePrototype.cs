using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._CE.Workbench.Prototypes;

[Prototype("CERecipe")]
public sealed partial class CEWorkbenchRecipePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CEWorkbenchRecipePrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    [DataField]
    public TimeSpan CraftTime = TimeSpan.FromSeconds(1f);

    [DataField]
    public SoundSpecifier? OverrideCraftSound;

    /// <summary>
    /// Mandatory conditions, without which the craft button will not even be active
    /// </summary>
    [DataField(required: true)]
    public List<CEResourceRequirement> Requirements = new();

    /// <summary>
    /// Mandatory conditions for completion, but not blocking the craft button.
    /// Players must monitor compliance themselves.
    /// If the conditions are not met, negative effects occur.
    /// </summary>
    [DataField]
    public List<CEWorkbenchCraftCondition> Conditions = new();

    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public int ResultCount = 1;

    [DataField]
    public ProtoId<CEWorkbenchRecipeCategoryPrototype>? Category;

    [DataField]
    public int Priority = 0;  // In descending order. More means it will be first.

    /// <summary>
    /// Skill the character must know to craft this recipe. If unset, the recipe is always
    /// craftable by everyone (no gate at all).
    /// </summary>
    [DataField]
    public ProtoId<CESkillPrototype>? RequiredSkill;

    /// <summary>
    /// Entity prototype of the workbench this recipe is crafted at, used only to build the
    /// player-facing guidebook text. Not used for actual crafting station matching - that's
    /// driven by <see cref="Tag"/> and the workbench's own recipe tag list.
    /// </summary>
    [DataField]
    public EntProtoId? Workbench;
}
