using Content.Shared._CE.EntityEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Prototypes;

/// <summary>
/// An action the player can take on a selected research map cell, e.g. "scan the surrounding area".
/// Shown as a button in the research table UI when <see cref="AllowedCells"/> matches the selected
/// cell's kind and every one of <see cref="Conditions"/> passes for the player. The client only
/// uses this to decide which buttons to show - the server re-validates both before running
/// <see cref="Effects"/>.
/// </summary>
[Prototype("researchAction")]
public sealed partial class CEResearchActionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Shown in a tooltip when hovering over the action's button.
    /// </summary>
    [DataField]
    public LocId? Tooltip;

    [DataField(required: true)]
    public CEResearchCellKind AllowedCells;

    [DataField]
    public List<CEEntityCondition> Conditions = new();

    [DataField(required: true)]
    public List<CEResearchActionEffect> Effects = new();
}
