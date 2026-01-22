namespace Content.Shared._CE.ZLevels.Pulling;

/// <summary>
/// Temporary component added during z-level transitions to track the entity being pulled across z-levels.
/// </summary>
[RegisterComponent]
public sealed partial class CEZLevelActivePullerComponent : Component
{
    public EntityUid PulledEnt;
}
