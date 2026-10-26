using Content.Shared._DND.Abilities;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Spells;

/// <summary>
/// Prepared spells, cantrips, and the shared pool of spell slots.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndSpellcastingComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DndAbilityPrototype> Ability = DndAbilityPrototype.CharismaId;

    [DataField, AutoNetworkedField]
    public DndFocusKind Focus = DndFocusKind.None;

    [DataField, AutoNetworkedField]
    public int Slots;

    [DataField, AutoNetworkedField]
    public int MaxSlots;

    [DataField, AutoNetworkedField]
    public int MaxPrepared;

    [DataField]
    public string SpellList = "";

    [DataField]
    public bool ReprepareOnLongRest;

    /// <summary>
    /// Until this time, a finished long rest can still change prepared spells.
    /// </summary>
    [DataField]
    public TimeSpan ReprepareUntil;

    [DataField]
    public List<ProtoId<DndSpellPrototype>> Cantrips = new();

    [DataField]
    public List<ProtoId<DndSpellPrototype>> Prepared = new();

    /// <summary>
    /// Spells that stay prepared and do not count against <see cref="MaxPrepared"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<DndSpellPrototype>> AlwaysPrepared = new();

    /// <summary>
    /// Leveled spell actions, so they all show the same number of slots.
    /// </summary>
    public List<EntityUid> SlotActions = new();
}
