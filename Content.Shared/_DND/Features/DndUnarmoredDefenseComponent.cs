using Robust.Shared.GameStates;

namespace Content.Shared._DND.Features;

/// <summary>
/// Base Armor Class is 10 + Dexterity + Constitution while no body armor is worn. A shield still allows it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DndUnarmoredDefenseComponent : Component
{
}
