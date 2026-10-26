using Content.Shared._DND.Combat;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Spells;

public enum DndCastingTime
{
    Action,
    BonusAction,
}

public enum DndSpellEffect
{
    Mockery,
    Lights,
    Attack,
    SaveDamage,
    Heal,
    Charm,
    ConeBlind,
    Whisper,
    Thunderwave,
    FaerieFire,
    Bane,
    Heroism,
    Sleep,
    Longstrider,
    Guidance,
    Smite,
    Mend,
    Ward,
    SpareDying,
    Wonder,
    Bless,
    Command,
    GuidingBolt,
    ShieldOfFaith,
    Sanctuary,
    Protection,
    DetectSides,
    DetectMagic,
    DetectPoison,
    Splash,
    Purify,
    Druidcraft,
    Elementalism,
    Message,
    ProduceFlame,
    Shillelagh,
    AnimalFriendship,
    Entangle,
    Fog,
    Goodberry,
    IceKnife,
    Jump,
    SpeakWithAnimals,
}

/// <summary>
/// The eight schools of magic. Spell files are grouped by this, then by level.
/// </summary>
public enum DndSpellSchool
{
    Abjuration,
    Conjuration,
    Divination,
    Enchantment,
    Evocation,
    Illusion,
    Necromancy,
    Transmutation,
}

/// <summary>
/// A cantrip or a leveled spell. Level 0 is a cantrip.
/// </summary>
[Prototype("dndSpell")]
public sealed partial class DndSpellPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public LocId Description { get; private set; }

    [DataField]
    public int Level { get; private set; }

    [DataField(required: true)]
    public DndSpellSchool School { get; private set; }

    /// <summary>
    /// Class spell lists this spell belongs to, such as Bard and Cleric.
    /// </summary>
    [DataField(required: true)]
    public List<string> Lists { get; private set; } = new();

    /// <summary>
    /// Classes that offer this spell first when the character has not chosen yet.
    /// </summary>
    [DataField]
    public List<string> RecommendedFor { get; private set; } = new();

    /// <summary>
    /// Lower numbers are offered first when a new character has not chosen yet.
    /// </summary>
    [DataField]
    public int Order { get; private set; } = 10;

    [DataField]
    public DndCastingTime Casting { get; private set; } = DndCastingTime.Action;

    [DataField]
    public int RangeFeet { get; private set; } = 60;

    [DataField]
    public bool CanTargetSelf { get; private set; }

    [DataField]
    public bool RequiresFocus { get; private set; }

    /// <summary>
    /// Ability used for the saving throw. Empty when the spell does not ask for one.
    /// </summary>
    [DataField]
    public string Save { get; private set; } = "";

    [DataField]
    public bool HalfOnSave { get; private set; }

    [DataField]
    public int DamageDice { get; private set; }

    [DataField]
    public int DamageSides { get; private set; }

    [DataField]
    public DndDamageType DamageType { get; private set; } = DndDamageType.Psychic;

    [DataField]
    public int HealDice { get; private set; }

    [DataField]
    public int HealSides { get; private set; }

    [DataField]
    public bool AddCastingModifier { get; private set; }

    [DataField(required: true)]
    public DndSpellEffect Effect { get; private set; }
}
