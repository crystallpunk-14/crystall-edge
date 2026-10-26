using Robust.Shared.GameStates;

namespace Content.Shared._DND.Classes;

/// <summary>
/// Bonus to Intelligence (Arcana) and Intelligence (Religion) checks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndThaumaturgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Bonus = 1;

    [DataField, AutoNetworkedField]
    public string FirstSkillId = "Arcana";

    [DataField, AutoNetworkedField]
    public string SecondSkillId = "Religion";

    [DataField, AutoNetworkedField]
    public string FirstSkill = "dnd-skill-arcana-name";

    [DataField, AutoNetworkedField]
    public string SecondSkill = "dnd-skill-religion-name";
}
