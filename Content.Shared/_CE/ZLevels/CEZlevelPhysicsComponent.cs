using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Allows an entity to move up and down the z-levels by gravity or jumping
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CESharedZLevelsSystem))]
public sealed partial class CEZLevelPhysicsComponent : Component
{
    /// <summary>
    /// The current speed of movement between z-levels.
    /// If greater than 0, the entity moves upward. If less than 0, the entity moves downward.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Velocity = 0f;

    /// <summary>
    /// The current height of the entity within the current Z-level.
    /// Takes values from 0 to 1. If the value rises above 1, the entity moves up to the next level and the value is normalized.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LocalHeight = 0f;
}
