namespace Content.Server._CE.WildMagic.Components;

/// <summary>
/// Game rule that seeds and maintains the round's wild magic node pool.
/// </summary>
[RegisterComponent, Access(typeof(CEWildMagicRuleSystem))]
public sealed partial class CEWildMagicRuleComponent : Component
{
    /// <summary>
    /// How many mandatory wild magic nodes should exist on the station at once.
    /// </summary>
    [DataField]
    public int NodeCount = 5;
}
