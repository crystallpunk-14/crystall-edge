using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Vampire.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(CESharedVampireSystem))]
public sealed partial class CEVampireComponent : Component
{
    /// <summary>
    /// The difference between higher and lower vampires is that lower vampires are unable to learn skills independently.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HigherVampire = true;

    [DataField]
    public ProtoId<ReagentPrototype> NewBloodReagent = "CEBloodVampire";

    [DataField]
    public ProtoId<CESkillTreePrototype> SkillTreeProto = "Vampire";

    [DataField]
    public ProtoId<MetabolizerTypePrototype> MetabolizerType = "CEVampire";

    [DataField]
    public ProtoId<CESkillPointPrototype> SkillPointProto = "Blood";

    [DataField]
    public FixedPoint2 SkillPointCount = 2f;

    [DataField]
    public TimeSpan ToggleVisualsTime = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// All this actions was granted to vampires on component added
    /// </summary>
    [DataField]
    public List<EntProtoId> ActionsProto = new() { "CEActionVampireToggleVisuals" };

    /// <summary>
    /// For tracking granted actions, and removing them when component is removed.
    /// </summary>
    [DataField]
    public List<EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    public float HeatUnderSunTemperature = 12000f;

    [DataField]
    public TimeSpan HeatFrequency = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextHeatTime = TimeSpan.Zero;

    [DataField]
    public float IgniteThreshold = 350f;
}
