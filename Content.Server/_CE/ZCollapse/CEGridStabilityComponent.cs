/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.Map;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Marks a grid that participates in ZCollapse. Added via a station's <c>zLevelsComponentOverrides</c>.
/// </summary>
[RegisterComponent]
public sealed partial class CEGridStabilityComponent : Component
{
    /// <summary>
    /// Last computed stability per tile. Missing tile = no stability.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> Stability = new();

    /// <summary>
    /// Anchored cores on this grid.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Cores = new();

    /// <summary>
    /// Anchored supports on this grid.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Supports = new();

    /// <summary>
    /// Unsupported tiles and their collapse time.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, TimeSpan> PendingCollapses = new();

    /// <summary>
    /// Warning dust queue, by dust time. Entries not matching <see cref="PendingCollapses"/> are stale and skipped.
    /// </summary>
    public readonly PriorityQueue<(Vector2i Tile, TimeSpan CollapseAt), TimeSpan> WarningQueue = new();

    /// <summary>
    /// Collapse queue, by collapse time. Stale entries skipped the same way.
    /// </summary>
    public readonly PriorityQueue<(Vector2i Tile, TimeSpan CollapseAt), TimeSpan> CollapseQueue = new();

    [ViewVariables]
    public TimeSpan NextRecomputeTime;

    /// <summary>
    /// Collapse sound cooldown per sound area.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, TimeSpan> NextSoundTime = new();

    /// <summary>
    /// If this grid is the lowest level of its network, all its tiles are seeded with
    /// <see cref="CEZCollapseSystem.LowestLevelSupportValue"/> stability.
    /// </summary>
    [DataField]
    public bool SupportLowestLevel;
}
