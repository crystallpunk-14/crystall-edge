using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.ZLevels.TileVolume;

/// <summary>
/// Owns <see cref="CETileVolumeOverlay"/> and the per-grid <see cref="CETileVolumeCacheComponent"/>:
/// builds chunk edge masks on demand and drops them when tiles change.
/// </summary>
public sealed partial class CEClientTileVolumeSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private SharedMapSystem _map = default!;

    /// <summary>
    /// Scratch buffer for <see cref="BuildChunk"/>, not state: cleared before every use.
    /// </summary>
    private readonly ulong[] _maskScratch = new ulong[CETileVolumeDirection.Count];

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, CCVars.CEZLevelsRenderingTileVolume, OnEnabledChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CETileVolumeOverlay>();
    }

    private void OnEnabledChanged(bool enabled)
    {
        if (enabled)
        {
            if (!_overlay.HasOverlay<CETileVolumeOverlay>())
                _overlay.AddOverlay(new CETileVolumeOverlay());
        }
        else
        {
            _overlay.RemoveOverlay<CETileVolumeOverlay>();
        }
    }

    [SubscribeLocalEvent]
    private void OnTileChanged(Entity<CETileVolumeCacheComponent> ent, ref TileChangedEvent args)
    {
        if (args.Changes.Length == 0)
        {
            ent.Comp.Chunks.Clear();
            return;
        }

        // A tile change also flips the edge bits of its neighbours, which may live in adjacent chunks.
        foreach (var change in args.Changes)
        {
            ent.Comp.Chunks.Remove(SharedMapSystem.GetChunkIndices(change.GridIndices, CETileVolumeChunk.Size));

            foreach (var offset in CETileVolumeDirection.Offsets)
            {
                ent.Comp.Chunks.Remove(SharedMapSystem.GetChunkIndices(change.GridIndices + offset, CETileVolumeChunk.Size));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<ContentTileDefinition>())
            return;

        if (_overlay.TryGetOverlay<CETileVolumeOverlay>(out var overlay))
            overlay.ClearVolumeSprites();

        var query = EntityQueryEnumerator<CETileVolumeCacheComponent>();
        while (query.MoveNext(out var cache))
        {
            cache.Chunks.Clear();
        }
    }

    public CETileVolumeCacheComponent EnsureCache(EntityUid grid)
    {
        return EnsureComp<CETileVolumeCacheComponent>(grid);
    }

    public CETileVolumeChunk GetChunk(Entity<MapGridComponent> grid, CETileVolumeCacheComponent cache, Vector2i chunkIndices)
    {
        if (cache.Chunks.TryGetValue(chunkIndices, out var chunk))
            return chunk;

        chunk = BuildChunk(grid, chunkIndices);
        cache.Chunks[chunkIndices] = chunk;
        return chunk;
    }

    private CETileVolumeChunk BuildChunk(Entity<MapGridComponent> grid, Vector2i chunkIndices)
    {
        var origin = chunkIndices * CETileVolumeChunk.Size;
        var masks = _maskScratch;
        Array.Clear(masks);
        int[]? tileIds = null;

        for (var y = 0; y < CETileVolumeChunk.Size; y++)
        {
            for (var x = 0; x < CETileVolumeChunk.Size; x++)
            {
                var indices = origin + new Vector2i(x, y);
                if (!_map.TryGetTileRef(grid.Owner, grid.Comp, indices, out var tileRef) ||
                    CEZLevelOpeningCache.IsOpeningTile(tileRef.Tile, _tileDef))
                {
                    continue;
                }

                var bitIndex = y * CETileVolumeChunk.Size + x;
                var bit = 1UL << bitIndex;
                var hasLip = false;

                for (var dir = 0; dir < CETileVolumeDirection.Count; dir++)
                {
                    if (!CEZLevelOpeningCache.IsOpeningTile(grid, indices + CETileVolumeDirection.Offsets[dir], _map, _tileDef))
                        continue;

                    masks[dir] |= bit;
                    hasLip = true;
                }

                if (!hasLip)
                    continue;

                tileIds ??= new int[CETileVolumeChunk.Size * CETileVolumeChunk.Size];
                tileIds[bitIndex] = tileRef.Tile.TypeId;
            }
        }

        if (tileIds == null)
            return CETileVolumeChunk.Empty;

        var chunk = new CETileVolumeChunk(tileIds);
        Array.Copy(masks, chunk.Masks, CETileVolumeDirection.Count);
        return chunk;
    }
}
