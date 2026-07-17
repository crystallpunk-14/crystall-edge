using Robust.Shared.Map;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Opt-in marker for a grid/map that participates in the ZCollapse structural-stability system,
/// and the storage for its computed per-tile stability. Only grids with this component (added via
/// a station's <c>zLevelsComponentOverrides</c>) can grow a Core's stability field or collapse tiles
/// — player shuttles never opt in.
/// </summary>
[RegisterComponent]
public sealed partial class CEGridStabilityComponent : Component
{
    /// <summary>
    /// Current computed stability per tile. A tile absent from this dictionary has stability &lt;= 0
    /// (dead/untouched) and is never colored by the debug overlay.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> Stability = new();

    /// <summary>
    /// Combined seed value per tile — always the max of <see cref="CoreSeeds"/>,
    /// <see cref="BridgeSeedsFromAbove"/> and <see cref="BridgeSeedsFromBelow"/>. This is the ground
    /// truth de-propagation re-floods from — <see cref="Stability"/> alone can't distinguish
    /// "propagated from a neighbor" from "independently sourced".
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> Seeds = new();

    /// <summary>
    /// Seed contribution from a <see cref="CEGridStabilityCoreComponent"/> anchored on this tile.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> CoreSeeds = new();

    /// <summary>
    /// Seed contribution from a <see cref="CEGridStabilitySupportComponent"/> anchored on THIS tile,
    /// pulling stability down from the level above. Written only by this grid's own
    /// <see cref="CEZCollapseSystem.RecomputeBridge"/> call.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> BridgeSeedsFromAbove = new();

    /// <summary>
    /// Seed contribution pushed up from a <see cref="CEGridStabilitySupportComponent"/> anchored one
    /// level below this tile. Written only by the level-below grid's
    /// <see cref="CEZCollapseSystem.RecomputeBridge"/> call.
    /// </summary>
    /// <remarks>
    /// Kept in a separate dictionary from <see cref="BridgeSeedsFromAbove"/> rather than one shared
    /// "BridgeSeeds" slot: a support directly on this tile and a support one level below both write
    /// through <see cref="CEZCollapseSystem.RecomputeBridge"/>, but for two different physical
    /// supports. Sharing one slot meant whichever of those two calls ran last on a given tick won and
    /// silently discarded the other's contribution — the recheck for a support directly on this tile
    /// (usually absent) would clobber a real value just pushed up from the support below, and vice
    /// versa. That caused both a wall's "feed up" value to snap back to a stale reading and phantom
    /// seeds to survive a Core's removal, because the wrong writer kept re-asserting them.
    /// </remarks>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> BridgeSeedsFromBelow = new();
}
