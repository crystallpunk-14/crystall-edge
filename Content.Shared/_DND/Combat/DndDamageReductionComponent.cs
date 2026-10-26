using Robust.Shared.GameStates;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Resistance halves damage of that type (round down). Vulnerability doubles it.
/// Several resistances to the same type, including resistance to all damage, count as one.
/// Modifiers apply first, then resistance, then vulnerability. Immunity deals no damage of that type.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndDamageReductionComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<DndDamageType> Resistances = new();

    [DataField, AutoNetworkedField]
    public List<DndDamageType> Vulnerabilities = new();

    [DataField, AutoNetworkedField]
    public List<DndDamageType> Immunities = new();

    [DataField, AutoNetworkedField]
    public bool ResistAll;

    [DataField, AutoNetworkedField]
    public bool VulnerableAll;

    [DataField, AutoNetworkedField]
    public bool ImmuneAll;
}
