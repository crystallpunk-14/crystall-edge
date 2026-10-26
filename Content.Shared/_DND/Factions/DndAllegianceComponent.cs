using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Factions;

/// <summary>
/// The side this creature belongs to. Adventurers, goblins, and wolves each refuse to harm their own.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndAllegianceComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DndSidePrototype> Side;
}
