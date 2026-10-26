using Robust.Shared.GameStates;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Armor Class that replaces the base 10 + Dexterity calculation. Only one base is in effect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndArmorClassComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ArmorClass = DndCombat.DefaultArmorClass;
}
