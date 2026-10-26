using Content.Shared._DND.Abilities;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Backgrounds;

/// <summary>
/// A character background. Its three abilities can be raised after the base scores are assigned.
/// </summary>
[Prototype("dndBackground")]
public sealed partial class DndBackgroundPrototype : IPrototype
{
    public const string Default = "Acolyte";

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public LocId Description { get; private set; }

    [DataField]
    public int Order { get; private set; }

    /// <summary>
    /// The three abilities this background can increase.
    /// </summary>
    [DataField]
    public List<ProtoId<DndAbilityPrototype>> Abilities = new();
}
