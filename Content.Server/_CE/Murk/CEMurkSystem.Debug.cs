using System.Linq;
using Content.Server._CE.MurkSphere;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.Events;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CE.Murk;

// Debug overlay networking for showmurkdebug - mirrors RadiationSystem.Debug.cs.
public sealed partial class CEMurkSystem
{
    [Dependency] private CEMurkPylonSystem _pylons = default!;
    [Dependency] private SharedTransformSystem _debugTransform = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<ICommonSession> _debugSessions = new();

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);
    private TimeSpan _nextRefresh = TimeSpan.Zero;

    public bool ToggleDebugView(ICommonSession session)
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

        RaiseNetworkEvent(new CEMurkDebugOverlayToggledEvent(isEnabled), session.Channel);

        if (isEnabled)
            RaiseNetworkEvent(BuildSnapshot(), session);

        return isEnabled;
    }

    private void PushDebugSnapshot()
    {
        if (_debugSessions.Count == 0)
            return;

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + RefreshInterval;

        var snapshot = BuildSnapshot();
        foreach (var session in _debugSessions.ToArray())
        {
            if (session.Status != SessionStatus.InGame)
                _debugSessions.Remove(session);
            else
                RaiseNetworkEvent(snapshot, session);
        }
    }

    // AllEntityQuery, not EntityQueryEnumerator - the latter silently skips EntityPaused entities,
    // and every entity on a znetwork-gamemap-mapping map is paused until znetwork-initialize.
    private CEMurkDebugOverlaySnapshotEvent BuildSnapshot()
    {
        var freeZones = new List<CEMurkDebugSource>();
        var sourceQuery = AllEntityQuery<CEMurkSourceComponent, TransformComponent>();
        while (sourceQuery.MoveNext(out _, out var source, out var xform))
        {
            if (!source.Active || xform.MapUid is not { } mapUid)
                continue;

            freeZones.Add(new CEMurkDebugSource(GetNetEntity(mapUid), _debugTransform.GetWorldPosition(xform), source.Intensity));
        }

        var boundaries = new List<CEMurkDebugSource>();
        var boundaryQuery = AllEntityQuery<CEMurkBoundaryComponent, TransformComponent>();
        while (boundaryQuery.MoveNext(out _, out var boundary, out var xform))
        {
            if (!boundary.Active || xform.MapUid is not { } mapUid)
                continue;

            boundaries.Add(new CEMurkDebugSource(GetNetEntity(mapUid), _debugTransform.GetWorldPosition(xform), -boundary.Radius));
        }

        var pylons = new List<CEMurkDebugPylon>();
        foreach (var (_, xform, tooClose) in _pylons.GetPylonDebugInfo())
        {
            if (xform.MapUid is not { } mapUid)
                continue;

            pylons.Add(new CEMurkDebugPylon(GetNetEntity(mapUid), _debugTransform.GetWorldPosition(xform), tooClose));
        }

        return new CEMurkDebugOverlaySnapshotEvent(freeZones, boundaries, CEMurkPylonSystem.PylonsMinRadius, pylons);
    }
}
