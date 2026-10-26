using Robust.Shared.GameStates;

namespace Content.Shared._DND.Classes;

/// <summary>
/// Knows Druidic, including how to read a hidden sign.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DndDruidicComponent : Component
{
}

/// <summary>
/// A hidden message. Someone who knows Druidic reads it. Anyone else only notices the mark with a check.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndDruidicSignComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Message = "";
}
