using System.Numerics;
using System.Threading;
using Content.Server.NPC.Pathfinding;
using Content.Shared.DoAfter;
using Content.Shared.NPC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.GOAP.Steering;

/// <summary>
/// Movement settings and state of a GOAP agent, driven by <see cref="CEGOAPSteeringSystem"/>.
/// The agent walks to its action targets through this component, so movement never appears as an action
/// in the plan. Based on the vanilla NPC steering, decoupled from HTN.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEGOAPSteeringComponent : Component
{
    #region Settings

    /// <summary>
    /// Climb over tables, railings and other climbable obstacles on the path.
    /// </summary>
    [DataField]
    public bool Climb;

    /// <summary>
    /// Break obstacles on the path with the agent's weapon.
    /// </summary>
    [DataField]
    public bool Smash;

    /// <summary>
    /// Pry doors open.
    /// </summary>
    [DataField]
    public bool Pry;

    /// <summary>
    /// Open doors that don't open on bump.
    /// </summary>
    [DataField]
    public bool Interact;

    /// <summary>
    /// Unbuckle from chairs, beds and the like before moving.
    /// </summary>
    [DataField]
    public bool Unbuckle = true;

    /// <summary>
    /// Break free when someone pulls the agent.
    /// </summary>
    [DataField]
    public bool Unpull = true;

    /// <summary>
    /// Open a locker or crate the agent is stuck in, or beat it with the agent's weapon if it won't open.
    /// </summary>
    [DataField]
    public bool EscapeStorage = true;

    /// <summary>
    /// How often a held agent retries freeing itself.
    /// </summary>
    [DataField]
    public TimeSpan FreeRetryInterval = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// How far the destination must move before the path is rebuilt.
    /// </summary>
    [DataField]
    public float ReregisterThreshold = 1f;

    #endregion

    #region Destination

    /// <summary>
    /// Final destination the agent is heading to. Null while the agent stands still.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? Target;

    /// <summary>
    /// How close the agent needs to get to <see cref="Target"/>.
    /// </summary>
    [ViewVariables]
    public float TargetRange;

    /// <summary>
    /// Downhill direction of the slope the agent walks to in order to go one Z-level up.
    /// </summary>
    [ViewVariables]
    public Direction? PendingAscent;

    /// <summary>
    /// Downhill direction and map below of the slope the agent walks to in order to go one Z-level down.
    /// </summary>
    [ViewVariables]
    public (Direction SlopeDir, EntityUid BelowMap)? PendingDescent;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextFreeAttempt;

    #endregion

    #region Steering

    /// <summary>
    /// Whether the agent is currently steering towards <see cref="Coordinates"/>.
    /// </summary>
    [ViewVariables]
    public bool Steering;

    /// <summary>
    /// Point the agent is steering to: the destination itself or a slope on the way to another Z-level.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates Coordinates;

    /// <summary>
    /// How close the agent needs to get to <see cref="Coordinates"/>.
    /// </summary>
    [ViewVariables]
    public float Range = 0.2f;

    [ViewVariables]
    public CEGOAPSteeringStatus Status = CEGOAPSteeringStatus.Moving;

    /// <summary>
    /// Used to override seeking behavior for context steering.
    /// </summary>
    [ViewVariables]
    public bool CanSeek = true;

    /// <summary>
    /// Radius for collision avoidance.
    /// </summary>
    [ViewVariables]
    public float Radius = 0.35f;

    [ViewVariables]
    public float[] Interest = new float[SharedNPCSteeringSystem.InterestDirections];

    [ViewVariables]
    public float[] Danger = new float[SharedNPCSteeringSystem.InterestDirections];

    // Debug only.
    public readonly List<Vector2> DangerPoints = new();

    [ViewVariables]
    public Vector2 LastSteerDirection = Vector2.Zero;

    /// <summary>
    /// Last position we considered for being stuck.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates LastStuckCoordinates;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastStuckTime;

    public const float StuckDistance = 1f;

    /// <summary>
    /// Have we currently requested a path.
    /// </summary>
    [ViewVariables]
    public bool Pathfind => PathfindToken != null;

    [ViewVariables]
    public CancellationTokenSource? PathfindToken;

    /// <summary>
    /// Current path we're following to our coordinates.
    /// </summary>
    [ViewVariables]
    public Queue<PathPoly> CurrentPath = new();

    /// <summary>
    /// How far does the last node in the path need to be before considering re-pathfinding.
    /// </summary>
    [ViewVariables]
    public float RepathRange = 1.5f;

    public const int FailedPathLimit = 3;

    /// <summary>
    /// How many times we've failed to pathfind. Once this hits the limit we'll stop steering.
    /// </summary>
    [ViewVariables]
    public int FailedPathCount;

    /// <summary>
    /// If the agent is using a do_after to clear an obstacle.
    /// </summary>
    [ViewVariables]
    public DoAfterId? DoAfterId;

    #endregion
}

public enum CEGOAPSteeringStatus : byte
{
    /// <summary>
    /// The agent is within range of its destination.
    /// </summary>
    InRange,

    /// <summary>
    /// The agent is moving towards its destination or freeing itself to do so.
    /// </summary>
    Moving,

    /// <summary>
    /// The destination can't be reached.
    /// </summary>
    NoPath,
}
