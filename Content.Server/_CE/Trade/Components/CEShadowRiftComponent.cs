using Robust.Shared.Prototypes;

namespace Content.Server._CE.Trade.Components;

/// <summary>
/// Home of shadow traders. At every dawn it dissolves the traders it released before and lets out a new batch.
/// </summary>
[RegisterComponent, Access(typeof(CEShadowRiftSystem))]
public sealed partial class CEShadowRiftComponent : Component
{
    /// <summary>
    /// Traders to pick from for each spawn.
    /// </summary>
    [DataField]
    public List<EntProtoId> Spawns = new();

    [DataField]
    public int Count = 1;

    /// <summary>
    /// Delay between two traders coming out.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Spawned where a trader dissolves.
    /// </summary>
    [DataField]
    public EntProtoId DissolveEffect = "CELurkerShadowStepEffect";

    [ViewVariables]
    public List<EntityUid> Traders = new();

    /// <summary>
    /// Traders still waiting to come out.
    /// </summary>
    [ViewVariables]
    public int PendingSpawns;

    [ViewVariables]
    public TimeSpan NextSpawnAt;
}
