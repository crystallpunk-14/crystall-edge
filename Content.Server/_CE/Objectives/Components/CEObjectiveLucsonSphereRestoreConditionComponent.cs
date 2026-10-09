namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks the city's Lucson Sphere restoration objective - see <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveLucsonSphereRestoreConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveLucsonSphereRestoreConditionComponent : Component
{
    /// <summary>
    /// How often to push the current charge into the objective's networked progress. Charge
    /// itself updates every tick, but the character menu doesn't need updates that often.
    /// </summary>
    [DataField]
    public TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Time remaining until the next refresh - see <see cref="RefreshInterval"/>.
    /// </summary>
    [DataField]
    public TimeSpan NextRefresh = TimeSpan.Zero;
}
