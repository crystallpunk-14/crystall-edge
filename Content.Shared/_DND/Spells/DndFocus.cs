namespace Content.Shared._DND.Spells;

public enum DndFocusKind
{
    None,
    Instrument,
    HolySymbol,
    Druidic,
}

/// <summary>
/// A worn or held item that can focus spells of this kind.
/// </summary>
[RegisterComponent]
public sealed partial class DndSpellFocusComponent : Component
{
    [DataField(required: true)]
    public DndFocusKind Kind = DndFocusKind.HolySymbol;
}
