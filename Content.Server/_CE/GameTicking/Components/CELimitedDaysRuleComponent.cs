namespace Content.Server._CE.GameTicking.Components;

/// <summary>
/// Limits the round time to the specified number of days
/// </summary>
[RegisterComponent, Access(typeof(CELimitedDaysRuleSystem))]
public sealed partial class CELimitedDaysRuleComponent : Component
{
    /// <summary>
    /// How many days (inclusive) must end before the end of the round?
    /// </summary>
    [DataField]
    public int Max = 3;

    /// <summary>
    /// how many days have already passed in the current round, while this rule active
    /// </summary>
    [DataField]
    public int Current = 0;
}
