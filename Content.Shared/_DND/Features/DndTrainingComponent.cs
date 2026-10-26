using Content.Shared._DND.Abilities;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Features;

/// <summary>
/// Weapon, armor, save, and skill training gained from a class.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndTrainingComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<DndAbilityPrototype>> SavingThrows = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<DndSkillPrototype>> Skills = new();

    [DataField, AutoNetworkedField]
    public List<DndWeaponTraining> Weapons = new();

    [DataField, AutoNetworkedField]
    public List<DndArmorCategory> Armor = new();
}
