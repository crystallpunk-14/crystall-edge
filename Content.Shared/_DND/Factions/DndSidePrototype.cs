using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Factions;

/// <summary>
/// A group such as Adventurers, Goblins, or Wolves. Creatures on the same side do not harm each other.
/// </summary>
[Prototype("dndSide")]
public sealed partial class DndSidePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }
}
