using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Prototypes;

/// <summary>
/// An action the player can take on a selected research map cell, e.g. "scan the surrounding area".
/// Shown as an entry in the research table UI when <see cref="AllowedCells"/> matches the selected
/// cell's kind and every one of <see cref="Conditions"/> passes for the player. The client only
/// uses this to decide which entries to show and whether the button is enabled - the server
/// re-validates everything (including <see cref="Cost"/>) before running <see cref="Effects"/>.
/// </summary>
[Prototype("researchAction")]
public sealed partial class CEResearchActionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Shown as the action button's tooltip.
    /// </summary>
    [DataField]
    public LocId? Desc;

    /// <summary>
    /// How many research points of each essence type this action costs. Spent from the player's
    /// research data when the action is executed. Actions whose effects derive their own cost
    /// elsewhere (e.g. <see cref="Content.Shared._CE.Science.Effects.CEResearchDiscoverAchievement"/>,
    /// which uses the target achievement's cost) leave this empty.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Cost = new();

    [DataField(required: true)]
    public CEResearchCellKind AllowedCells;

    [DataField]
    public List<CEEntityCondition> Conditions = new();

    [DataField(required: true)]
    public List<CEResearchActionEffect> Effects = new();
}
