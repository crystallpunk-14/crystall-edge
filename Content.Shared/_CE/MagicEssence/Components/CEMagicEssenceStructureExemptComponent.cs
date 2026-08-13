namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// Marks an item as intentionally exempt from <c>CEMagicEssenceStructureTest</c>'s "every CE item
/// carries essence" check. For items whose essence reading already comes entirely from elsewhere -
/// e.g. a solution pre-filled with an essence reagent (the reagent-embodiment path in
/// <see cref="Content.Shared._CE.MagicEssence.Systems.CEMagicEssenceSystem"/>) - so adding
/// <see cref="CEMagicEssenceStructureComponent"/> too would just double-count on top of that.
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicEssenceStructureExemptComponent : Component;
