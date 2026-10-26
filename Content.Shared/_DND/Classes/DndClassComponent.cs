using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Classes;

/// <summary>
/// The class a spawned character chose during creation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndClassComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DndClassPrototype> Class = DndClassPrototype.Default;
}
