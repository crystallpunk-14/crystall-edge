using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Ladder;

/// <summary>
/// Added to a mob while it is in contact with a ladder, removed once it lets go.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CELadderSystem))]
public sealed partial class CELadderClimberComponent : Component
{
    /// <summary>
    /// Ladders on the climber's own level whose sensor fixture it currently touches.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Ladders = new();

    /// <summary>
    /// Ladder on the level below that the climber has just climbed over. Keeps holding the climber until
    /// it steps off the ladder's tile or stands on the floor.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BelowLadder;
}
