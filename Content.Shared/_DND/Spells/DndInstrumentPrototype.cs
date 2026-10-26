using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Spells;

/// <summary>
/// A musical instrument a bard can be proficient with and use as a spellcasting focus.
/// </summary>
[Prototype("dndInstrument")]
public sealed partial class DndInstrumentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// Entity spawned when this instrument is the one in the starting gear.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity { get; private set; }
}
