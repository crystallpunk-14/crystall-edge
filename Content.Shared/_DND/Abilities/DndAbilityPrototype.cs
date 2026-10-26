using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Abilities;

/// <summary>
/// One of the six abilities. The score is chosen in character creation; the modifier is derived from it.
/// </summary>
[Prototype("dndAbility")]
public sealed partial class DndAbilityPrototype : IPrototype
{
    public const string StrengthId = "Strength";
    public const string DexterityId = "Dexterity";
    public const string ConstitutionId = "Constitution";
    public const string IntelligenceId = "Intelligence";
    public const string WisdomId = "Wisdom";
    public const string CharismaId = "Charisma";

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// What the ability measures.
    /// </summary>
    [DataField(required: true)]
    public LocId Description { get; private set; }

    /// <summary>
    /// Display order in character creation. Lower comes first.
    /// </summary>
    [DataField]
    public int Order { get; private set; }

    /// <summary>
    /// Score used when a character has not chosen one yet.
    /// </summary>
    [DataField]
    public int DefaultScore { get; private set; } = 10;

    /// <summary>
    /// Lowest score character creation accepts.
    /// </summary>
    [DataField]
    public int MinScore { get; private set; } = 1;

    /// <summary>
    /// Highest score an adventurer can have unless a feature says otherwise.
    /// </summary>
    [DataField]
    public int MaxScore { get; private set; } = 20;
}
