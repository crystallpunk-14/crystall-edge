namespace Content.Shared._CE.EntityTable.Components;

/// <summary>
/// Marks an entity prototype as a candidate for random selection by <see cref="EntitySelectors.CEEntityGroupSelector"/>.
/// Place on an abstract base prototype to have it automatically inherited by all its children.
/// </summary>
[RegisterComponent]
public sealed partial class CEEntityGroupMemberComponent : Component
{
    /// <summary>
    /// Groups this entity belongs to, and its weight within each group.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<string, float> Groups = new();
}
