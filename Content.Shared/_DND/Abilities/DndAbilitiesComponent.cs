using Robust.Shared.GameStates;

namespace Content.Shared._DND.Abilities;

/// <summary>
/// Ability scores and level for a spawned character. Proficiency bonus and base AC are derived from these.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndAbilitiesComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Level = DndAbilityScores.StartingLevel;

    [DataField, AutoNetworkedField]
    public int Strength = 10;

    [DataField, AutoNetworkedField]
    public int Dexterity = 10;

    [DataField, AutoNetworkedField]
    public int Constitution = 10;

    [DataField, AutoNetworkedField]
    public int Intelligence = 10;

    [DataField, AutoNetworkedField]
    public int Wisdom = 10;

    [DataField, AutoNetworkedField]
    public int Charisma = 10;

    public int GetScore(string abilityId)
    {
        return abilityId switch
        {
            DndAbilityPrototype.StrengthId => Strength,
            DndAbilityPrototype.DexterityId => Dexterity,
            DndAbilityPrototype.ConstitutionId => Constitution,
            DndAbilityPrototype.IntelligenceId => Intelligence,
            DndAbilityPrototype.WisdomId => Wisdom,
            DndAbilityPrototype.CharismaId => Charisma,
            _ => 10,
        };
    }

    public int ProficiencyBonus => DndAbilityScores.ProficiencyBonus(Level);

    public int BaseArmorClass => DndAbilityScores.BaseArmorClass(Dexterity);
}
