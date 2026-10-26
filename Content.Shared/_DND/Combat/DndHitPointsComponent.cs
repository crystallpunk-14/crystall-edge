using Robust.Shared.GameStates;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Maximum hit points from the class hit die, Constitution, and level.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndHitPointsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Max = 1;
}
