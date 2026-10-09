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
}
