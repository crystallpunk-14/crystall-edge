using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Workbench;

/// <summary>
/// This entity can be used to craft other objects through the interface
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchComponent : Component
{
    /// <summary>
    /// Crafting speed modifier on this workbench.
    /// </summary>
    [DataField]
    public float CraftSpeed = 1f;

    /// <summary>
    /// List of recipes available for crafting on this type of workbench
    /// </summary>
    [DataField]
    public List<ProtoId<CEWorkbenchRecipePrototype>> Recipes = new();

    /// <summary>
    /// Auto recipe list fill based on tags
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> RecipeTags = new();

    [DataField]
    public ProtoId<CEWorkbenchRecipePrototype>? SelectedRecipe;

    /// <summary>
    /// Played during crafting. Can be overwritten by the crafting sound of a specific recipe.
    /// </summary>
    [DataField]
    public SoundSpecifier? CraftSound;

    /// <summary>
    /// Entity that will be spawned when crafting is complete
    /// </summary>
    [DataField]
    public EntProtoId? Vfx;
}
