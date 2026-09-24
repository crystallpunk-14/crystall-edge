using System.Numerics;
using Content.Shared._CE.Murk.Events;
using Content.Shared._CE.ZLevels.Core.EntitySystems;

namespace Content.Client._CE.Murk;

// Debug overlay client side for showmurkdebug - caches the server snapshot, projects it per map on demand.
public sealed partial class CEClientMurkSystem
{
    [Dependency] private CESharedZLevelsSystem _debugZLevels = default!;

    private List<CEMurkDebugSource> _freeZoneSources = new();
    private List<CEMurkDebugSource> _boundarySources = new();

    public float PylonRadius;
    public List<CEMurkDebugPylon> Pylons = new();

    [SubscribeNetworkEvent]
    private void OnDebugOverlayToggled(CEMurkDebugOverlayToggledEvent ev)
    {
        if (ev.IsEnabled)
            _overlayMgr.AddOverlay(new CEMurkDebugOverlay());
        else
            _overlayMgr.RemoveOverlay<CEMurkDebugOverlay>();
    }

    [SubscribeNetworkEvent]
    private void OnDebugOverlaySnapshot(CEMurkDebugOverlaySnapshotEvent ev)
    {
        _freeZoneSources = ev.FreeZoneSources;
        _boundarySources = ev.BoundarySources;
        PylonRadius = ev.PylonRadius;
        Pylons = ev.Pylons;
    }

    public bool BuildDebugBuffer(EntityUid targetMap, CEMurkDebugBuffer buffer)
    {
        buffer.FreeCount = ProjectLayer(targetMap, _freeZoneSources, buffer.FreePositions, buffer.FreeRadii, buffer.FreeStrengths);
        buffer.WallCount = ProjectLayer(targetMap, _boundarySources, buffer.WallPositions, buffer.WallRadii, buffer.WallStrengths);

        return buffer.FreeCount > 0 || buffer.WallCount > 0;
    }

    private int ProjectLayer(EntityUid targetMap, List<CEMurkDebugSource> sources, Vector2[] positions, float[] radii, float[] strengths)
    {
        var count = 0;

        foreach (var source in sources)
        {
            if (count >= CEMurkDebugBuffer.MaxCount)
                break;

            var sourceMap = GetEntity(source.Map);
            if (!_debugZLevels.TryGetZLevelOffset(targetMap, sourceMap, out var zOffset))
                continue;

            if (!TryProjectRadius(zOffset, source.Strength, out var radius))
                continue;

            positions[count] = source.WorldPos;
            radii[count] = radius;
            strengths[count] = source.Strength;
            count++;
        }

        return count;
    }

    // Same sphere-shrinking projection as everything else, so a pylon's exclusion radius shrinks
    // with z-distance instead of only ever showing on its own level.
    public bool TryProjectPylon(EntityUid targetMap, CEMurkDebugPylon pylon, out float radius)
    {
        radius = 0f;

        var sourceMap = GetEntity(pylon.Map);
        if (!_debugZLevels.TryGetZLevelOffset(targetMap, sourceMap, out var zOffset))
            return false;

        return TryProjectRadius(zOffset, PylonRadius, out radius);
    }
}
