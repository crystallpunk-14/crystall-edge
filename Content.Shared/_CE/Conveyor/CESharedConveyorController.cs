using System.Numerics;
using Content.Shared.Conveyor;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Physics.Controllers;

/// <inheritdoc/>
public abstract partial class SharedConveyorController
{
    /// <summary>
    /// How long an entity may be pushed without moving forward before the conveyor gives up on it.
    /// </summary>
    private const float CEStallTime = 0.5f;

    /// <summary>
    /// Forward speed, as a fraction of the conveyor speed, below which the entity counts as not moving.
    /// </summary>
    private const float CEStallSpeedRatio = 0.1f;

    /// <summary>
    /// Tracks entities the conveyor pushes into something it can't detect as a wall (e.g. a funnel edge).
    /// Stalled entities stop being pushed so they can fall asleep instead of staying awake forever.
    /// </summary>
    /// <returns>True if the entity is stalled and should be treated as blocked.</returns>
    private bool CEUpdateStall(EntityUid uid, ConveyedComponent conveyed, PhysicsComponent physics, Vector2 direction, float frameTime)
    {
        var speed = direction.Length();

        // Sleeping bodies get a fresh attempt once something wakes them
        if (!physics.Awake || speed <= 0f || _mover.UsedMobMovement.TryGetValue(uid, out var usedMob) && usedMob)
        {
            conveyed.StallTime = 0f;
            return false;
        }

        var forwardSpeed = Vector2.Dot(physics.LinearVelocity, direction) / speed;
        if (forwardSpeed > speed * CEStallSpeedRatio)
        {
            conveyed.StallTime = 0f;
            return false;
        }

        conveyed.StallTime += frameTime;
        return conveyed.StallTime >= CEStallTime;
    }
}
