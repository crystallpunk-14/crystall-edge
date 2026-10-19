namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks the city's Lucson Sphere restoration objective: its description lists the round's prices,
/// and it completes only when the Restoration Ritual succeeds.
/// See <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveLucsonSphereRestoreConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveLucsonSphereRestoreConditionComponent : Component
{
    /// <summary>
    /// Opening line of the description, followed by one line per round price.
    /// </summary>
    [DataField]
    public LocId Description = "ce-objective-city-restore-desc";

    /// <summary>
    /// Shown instead of the price list until the round's prices are rolled.
    /// </summary>
    [DataField]
    public LocId UnknownPrices = "ce-objective-city-restore-desc-unknown";
}
