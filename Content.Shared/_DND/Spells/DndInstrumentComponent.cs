namespace Content.Shared._DND.Spells;

/// <summary>
/// Marks the item as a musical instrument, so it can focus a bard spell.
/// </summary>
[RegisterComponent]
public sealed partial class DndInstrumentComponent : Component
{
    [DataField(required: true)]
    public string Kind = "";
}
