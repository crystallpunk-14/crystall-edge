using Content.Shared._DND.Classes;
using Robust.Shared.GameStates;

namespace Content.Shared._DND.Spells;

/// <summary>
/// A Bardic Inspiration die waiting to be rolled. A creature holds only one.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndBardicInspirationComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Sides = 6;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}

/// <summary>
/// The bard's remaining uses. The number on the action matches <see cref="Uses"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndBardicInspirationSourceComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Uses;

    [DataField, AutoNetworkedField]
    public int MaxUses;

    [DataField, AutoNetworkedField]
    public int DieSides = 6;

    public EntityUid? Action;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndFaerieFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndBaneComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndBlindComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndTemporaryHitPointsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Amount;

    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndLongstriderComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndCharmComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent]
public sealed partial class DndDancingLightComponent : Component
{
    [DataField]
    public TimeSpan ExpiresAt;
}

/// <summary>
/// The next attack or save adds 1d4, then this ends.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndGuidanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndBlessComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndShieldOfFaithComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Bonus = DndClericRules.ShieldOfFaithBonus;

    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

/// <summary>
/// Resistance to bludgeoning, piercing, and slashing until the time passes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndWardComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndSanctuaryComponent : Component
{
    [DataField, AutoNetworkedField]
    public int DifficultyClass = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

/// <summary>
/// Attacks from another side have disadvantage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndProtectionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

/// <summary>
/// Marks which spell an action button casts, so a long rest can swap prepared spells.
/// </summary>
[RegisterComponent]
public sealed partial class DndSpellActionComponent : Component
{
    [DataField]
    public string Spell = "";
}

/// <summary>
/// A club or quarterstaff uses Wisdom and its damage die is at least a d8.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndShillelaghComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

/// <summary>
/// The caster understands beasts for a while.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndSpeakWithAnimalsComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}

/// <summary>
/// A handful of berries. Each one restores 1 hit point.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndGoodberryComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Uses = 10;
}
