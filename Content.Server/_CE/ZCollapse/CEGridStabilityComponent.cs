using Robust.Shared.Map;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Opt-in marker for a grid/map that participates in the ZCollapse structural-stability system
/// (added via a station's <c>zLevelsComponentOverrides</c>). Only grids with this component can
/// grow a Core's stability field or collapse tiles — player shuttles never opt in.
///
/// Holds only ground-truth-derived state: the last computed <see cref="Stability"/> result, and a
/// live index of which Core/Support entities are currently anchored on this grid. Neither can go
/// stale independently of the world — <see cref="Cores"/>/<see cref="Supports"/> are just "what's
/// anchored here right now" (kept in sync 1:1 with anchor/unanchor), and <see cref="Stability"/> is
/// always fully replaced by <see cref="CEZCollapseSystem"/> from a fresh flood-fill, never patched
/// incrementally. There is deliberately no separate seed cache to desync.
/// </summary>
[RegisterComponent]
public sealed partial class CEGridStabilityComponent : Component
{
    /// <summary>
    /// Last computed stability per tile. A tile absent from this dictionary has stability &lt;= 0
    /// (dead/untouched) and is never colored by the debug overlay.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> Stability = new();

    /// <summary>Currently anchored <see cref="CEGridStabilityCoreComponent"/> entities on this grid.</summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Cores = new();

    /// <summary>Currently anchored <see cref="CEGridStabilitySupportComponent"/> entities on this grid.</summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Supports = new();

    /// <summary>
    /// Tiles on this grid won't be reaped for lacking stability before this time, set on MapInit/grid
    /// split. Every grid starts with an empty <see cref="Stability"/>, so right after a fresh map load
    /// a floor with no Core of its own would otherwise read a not-yet-computed neighbor's stability as
    /// a genuine zero — indistinguishable from "that neighbor computed and really has nothing here" —
    /// and collapse before a multi-hop bridge cascade has had time to settle. See
    /// <see cref="CEZCollapseSystem"/>.
    /// </summary>
    [ViewVariables]
    public TimeSpan ProtectedUntil;
}
