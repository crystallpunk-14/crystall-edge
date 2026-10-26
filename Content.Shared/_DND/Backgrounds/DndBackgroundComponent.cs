using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Backgrounds;

/// <summary>
/// The background chosen at character creation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndBackgroundComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DndBackgroundPrototype> Background = DndBackgroundPrototype.Default;
}
