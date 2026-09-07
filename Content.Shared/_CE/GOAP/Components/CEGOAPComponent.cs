using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.GOAP.Components;

/// <summary>
/// CrystallEdge GOAP NPC Component. Contains goals, available actions, and sensors
/// for goal-oriented action planning AI.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEGOAPComponent : Component
{
    /// <summary>
    /// When true, the entity spawns in a sleeping state (with <see cref="CEGOAPSleepingComponent"/>)
    /// and must be explicitly woken by a trigger (damage, proximity, etc.).
    /// </summary>
    [DataField]
    public bool StartSleeping = false;

    /// <summary>
    /// List of goals this entity can pursue.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<CEGOAPGoal> Goals = new();

    /// <summary>
    /// Available actions this entity can perform.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<CEGOAPAction> Actions = new();

    /// <summary>
    /// Knowledge store populated by perceptors. Maps perceived entities to their
    /// last known state (position, time, source, expiry).
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, CEGOAPKnowledgeEntry> Knowledge = new();

    [DataField]
    public TimeSpan MemoryDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Current world state as perceived by this entity.
    /// Keys are condition prototype IDs, values are boolean states.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, bool> WorldState = new();

    /// <summary>
    /// Current plan being executed. Null if no plan.
    /// </summary>
    [ViewVariables]
    public List<CEGOAPAction> CurrentPlan = new();

    /// <summary>
    /// Index of the currently executing action in the plan.
    /// </summary>
    [ViewVariables]
    public int CurrentActionIndex;

    /// <summary>
    /// Whether the current action has had its startup event raised.
    /// </summary>
    [ViewVariables]
    public bool CurrentActionStarted;

    /// <summary>
    /// The currently active goal being pursued (index into Goals list, -1 if none).
    /// </summary>
    [ViewVariables]
    public int ActiveGoalIndex = -1;

    /// <summary>
    /// Time between re-planning attempts.
    /// </summary>
    [DataField]
    public TimeSpan PlanCooldown = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The next game time at which re-planning is allowed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPlanTime;
}
