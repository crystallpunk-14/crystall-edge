using System.Linq;
using Content.Shared._CE.ZCollapse.Events;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._CE.ZCollapse;

// Debug overlay networking — sends stability snapshots only to clients that toggled it on,
// mirrors Content.Server/Radiation/Systems/RadiationSystem.Debug.cs.
public sealed partial class CEZCollapseSystem
{
    private readonly HashSet<ICommonSession> _debugSessions = new();

    private void InitializeDebug()
    {
    }

    /// <summary>
    /// Toggles the ZCollapse debug overlay for a player, called from <c>showstabilitydebug</c>.
    /// </summary>
    public void ToggleDebugView(ICommonSession session)
    {
        bool isEnabled;
        if (_debugSessions.Add(session))
        {
            isEnabled = true;
        }
        else
        {
            _debugSessions.Remove(session);
            isEnabled = false;
        }

        RaiseNetworkEvent(new CEZCollapseOverlayToggledEvent(isEnabled), session.Channel);

        if (isEnabled)
            SendFullSnapshot(session);
    }

    private void SendFullSnapshot(ICommonSession session)
    {
        var dict = new Dictionary<NetEntity, Dictionary<Vector2i, int>>();
        var query = AllEntityQuery<CEGridStabilityComponent>();
        while (query.MoveNext(out var gridUid, out var comp))
        {
            if (_gridQuery.TryGetComponent(gridUid, out var grid))
                dict[GetNetEntity(gridUid)] = BuildOverlayTiles(gridUid, grid, comp);
        }

        RaiseNetworkEvent(new CEZCollapseOverlaySnapshotEvent(dict), session);
    }

    /// <summary>Pushes stability updates for grids touched this tick, only if anyone's watching.</summary>
    private void PushDirtySnapshots()
    {
        if (_debugSessions.Count == 0 || _debugDirtyGrids.Count == 0)
        {
            _debugDirtyGrids.Clear();
            return;
        }

        var dict = new Dictionary<NetEntity, Dictionary<Vector2i, int>>();
        foreach (var gridUid in _debugDirtyGrids)
        {
            if (_stabilityQuery.TryGetComponent(gridUid, out var comp) && _gridQuery.TryGetComponent(gridUid, out var grid))
                dict[GetNetEntity(gridUid)] = BuildOverlayTiles(gridUid, grid, comp);
        }

        _debugDirtyGrids.Clear();

        var ev = new CEZCollapseOverlaySnapshotEvent(dict);
        foreach (var session in _debugSessions.ToArray())
        {
            if (session.Status != SessionStatus.InGame)
                _debugSessions.Remove(session);
            else
                RaiseNetworkEvent(ev, session);
        }
    }

    /// <summary>
    /// Debug-overlay payload for a grid: <see cref="CEGridStabilityComponent.Stability"/> as-is, plus
    /// an explicit 0 entry for every tile that physically exists but isn't in Stability — those are
    /// tiles that should collapse (and will, once the map is initialized) so the overlay needs to
    /// show them in red rather than silently omitting them like truly-empty space.
    /// </summary>
    private Dictionary<Vector2i, int> BuildOverlayTiles(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp)
    {
        var result = new Dictionary<Vector2i, int>(comp.Stability);

        var enumerator = _map.GetAllTilesEnumerator(gridUid, grid);
        while (enumerator.MoveNext(out var tileRef))
        {
            result.TryAdd(tileRef.Value.GridIndices, 0);
        }

        return result;
    }
}
