namespace Content.Server._CE.GameTicking.Components;

[RegisterComponent, Access(typeof(CESurviveDaysRuleSystem))]
public sealed partial class CESurviveDaysRuleComponent : Component
{
    [DataField]
    public int DaysSurvived = 0;
}
