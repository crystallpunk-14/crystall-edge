using Content.Shared.Inventory;

namespace Content.Shared._CE.MagicFocus.Components;

/// <summary>
/// Marks a <see cref="CEMagicFocusComponent"/>-bearing entity as usable while worn as equipment
/// in one of the given inventory slots.
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicFocusClothingComponent : Component
{
    /// <summary>
    /// The slots this item must be equipped in for it to act as a magic focus.
    /// </summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.WITHOUT_POCKET;
}
