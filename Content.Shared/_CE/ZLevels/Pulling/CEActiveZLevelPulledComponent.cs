namespace Content.Shared._CE.ZLevels.Pulling;

/// <summary>
/// Temporary component added during z-level transitions to track the entity pulling this entity across z-levels.
/// </summary>
[RegisterComponent]
public sealed partial class CEZLevelActivePulledComponent : Component
{
    [DataField]
    public EntityUid PullerEnt;
}
