using Robust.Shared.GameStates;

namespace Content.Shared._DND.Features;

/// <summary>
/// Speed is reduced by 10 feet until the time passes. A second slow does not reduce it further.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndSlowComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}
