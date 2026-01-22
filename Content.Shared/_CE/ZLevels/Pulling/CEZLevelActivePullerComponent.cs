namespace Content.Shared._CE.ZLevels.Pulling;

/// <summary>
/// Component that indicates that an entity is currently pulling some other entity.
/// </summary>
[RegisterComponent]
public sealed partial class CEZLevelActivePullerComponent : Component
{
    public EntityUid PulledEnt;
}
