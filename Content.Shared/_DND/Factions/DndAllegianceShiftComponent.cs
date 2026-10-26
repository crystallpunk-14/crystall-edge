using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Factions;

/// <summary>
/// Replaces <see cref="DndAllegianceComponent"/> while it is present.
/// A seduced goblin or a dominated adventurer carries this until the effect ends.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndAllegianceShiftComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DndSidePrototype> Side;
}
