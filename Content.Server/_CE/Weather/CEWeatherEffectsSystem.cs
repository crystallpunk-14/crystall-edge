using Content.Server._CE.Weather.Components;
using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Weather;

/// <summary>
/// Periodically scans open-sky tiles under active weathers and raises <see cref="CEWeatherEntityAffectedEvent"/>
/// for every exposed entity, budget-limited per tick so it stays cheap on large maps.
/// Standalone system (not a WeatherSystem partial) so it lives entirely under _CE without touching vanilla files.
/// </summary>
public sealed partial class CEWeatherEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    private int _maxAffectedPerTick;
    private int _maxTilesScannedPerTick;

    /// <summary>
    /// Per-weather processing state for time-budgeted gathering and application.
    /// </summary>
    private readonly Dictionary<EntityUid, CEWeatherEffectProcessingState> _processingStates = new();

    /// <summary>
    /// Reusable buffer for entity lookups — avoids per-tile allocations.
    /// </summary>
    private readonly HashSet<EntityUid> _entityBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEWeatherEffectsComponent, ComponentInit>(OnWeatherEffectsInit);
        SubscribeLocalEvent<CEWeatherEffectsComponent, ComponentShutdown>(OnWeatherEffectsShutdown);

        Subs.CVar(_cfg, CCVars.CEWeatherMaxAffectedPerTick, val => _maxAffectedPerTick = val, true);
        Subs.CVar(_cfg, CCVars.CEWeatherMaxTilesScannedPerTick, val => _maxTilesScannedPerTick = val, true);
    }

    private void OnWeatherEffectsInit(Entity<CEWeatherEffectsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextEffectTime = _timing.CurTime + ent.Comp.MaxEffectFrequency;
    }

    private void OnWeatherEffectsShutdown(Entity<CEWeatherEffectsComponent> ent, ref ComponentShutdown args)
    {
        _processingStates.Remove(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, CEWeatherEffectsComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out _, out var effects, out var status))
        {
            if (_timing.CurTime < effects.NextEffectTime)
                continue;

            var freq = _random.Next(effects.MinEffectFrequency, effects.MaxEffectFrequency);
            effects.NextEffectTime += freq;

            if (status.AppliedTo is not { } mapUid || !TryComp<MapComponent>(mapUid, out var mapComp))
                continue;

            StartGatheringCycle(uid, mapUid, mapComp.MapId);
        }

        ProcessStates();
    }

    private void StartGatheringCycle(EntityUid weatherUid, EntityUid mapUid, MapId mapId)
    {
        var state = EnsureProcessingState(weatherUid);
        state.MapUid = mapUid;
        state.MapId = mapId;
        state.Phase = CEEffectProcessingPhase.Gathering;
        state.PendingEntities.Clear();
        state.ProcessedEntities.Clear();
        state.Grids.Clear();
        state.CurrentGridIndex = 0;
        state.TileEnumeratorValid = false;

        foreach (var grid in _mapManager.GetAllGrids(mapId))
        {
            state.Grids.Add(grid);
        }
    }

    private void ProcessStates()
    {
        foreach (var (weatherUid, state) in _processingStates)
        {
            if (state.Phase == CEEffectProcessingPhase.Idle)
                continue;

            if (!Exists(weatherUid))
            {
                state.Phase = CEEffectProcessingPhase.Idle;
                continue;
            }

            switch (state.Phase)
            {
                case CEEffectProcessingPhase.Gathering:
                    ProcessGathering(state);
                    break;

                case CEEffectProcessingPhase.Applying:
                    ProcessApplying(weatherUid, state);
                    break;
            }
        }
    }

    /// <summary>
    /// Tile-centric gathering: iterates grid tiles, checks weather exposure, collects affected entities.
    /// Budget-limited by <see cref="CCVars.CEWeatherMaxTilesScannedPerTick"/>.
    /// </summary>
    private void ProcessGathering(CEWeatherEffectProcessingState state)
    {
        var tilesScanned = 0;

        while (state.CurrentGridIndex < state.Grids.Count)
        {
            var grid = state.Grids[state.CurrentGridIndex];
            var gridUid = grid.Owner;
            var gridComp = grid.Comp;

            _roofQuery.TryGetComponent(gridUid, out var roofComp);

            if (!state.TileEnumeratorValid)
            {
                state.CurrentTileEnumerator = _mapSystem.GetAllTilesEnumerator(gridUid, gridComp);
                state.TileEnumeratorValid = true;
            }

            while (state.CurrentTileEnumerator.MoveNext(out var tileRef))
            {
                tilesScanned++;

                if (!_weather.CanWeatherAffect((gridUid, gridComp, roofComp), tileRef.Value))
                {
                    if (tilesScanned >= _maxTilesScannedPerTick)
                        return;
                    continue;
                }

                // Find all entities on this weather-exposed tile.
                _entityBuffer.Clear();
                _lookup.GetLocalEntitiesIntersecting(gridUid, tileRef.Value.GridIndices, _entityBuffer,
                    gridComp: gridComp);

                foreach (var entUid in _entityBuffer)
                {
                    // Deduplicate: entities spanning multiple tiles are only queued once.
                    if (state.ProcessedEntities.Add(entUid))
                        state.PendingEntities.Enqueue(entUid);
                }

                if (tilesScanned >= _maxTilesScannedPerTick)
                    return;
            }

            state.CurrentGridIndex++;
            state.TileEnumeratorValid = false;
        }

        // All grids/tiles scanned — transition to applying.
        state.Phase = state.PendingEntities.Count > 0
            ? CEEffectProcessingPhase.Applying
            : CEEffectProcessingPhase.Idle;
    }

    /// <summary>
    /// Drains the pending entities queue, raising <see cref="CEWeatherEntityAffectedEvent"/> for each.
    /// Budget-limited by <see cref="CCVars.CEWeatherMaxAffectedPerTick"/>.
    /// </summary>
    private void ProcessApplying(EntityUid weatherUid, CEWeatherEffectProcessingState state)
    {
        var processed = 0;

        while (state.PendingEntities.TryDequeue(out var targetUid))
        {
            if (!_xformQuery.TryGetComponent(targetUid, out var xform) || xform.MapUid != state.MapUid)
                continue;

            var ev = new CEWeatherEntityAffectedEvent(targetUid);
            RaiseLocalEvent(weatherUid, ref ev);

            processed++;
            if (processed >= _maxAffectedPerTick)
                return;
        }

        state.Phase = CEEffectProcessingPhase.Idle;
    }

    private CEWeatherEffectProcessingState EnsureProcessingState(EntityUid weatherUid)
    {
        if (!_processingStates.TryGetValue(weatherUid, out var state))
        {
            state = new CEWeatherEffectProcessingState();
            _processingStates[weatherUid] = state;
        }

        return state;
    }
}

/// <summary>
/// Processing state for a single weather entity's effect cycle.
/// Supports pause/resume across ticks for budgeted processing.
/// </summary>
internal sealed class CEWeatherEffectProcessingState
{
    public CEEffectProcessingPhase Phase = CEEffectProcessingPhase.Idle;

    public EntityUid MapUid;
    public MapId MapId;

    // Gathering state — supports pause/resume across ticks.
    public List<Entity<MapGridComponent>> Grids = new();
    public int CurrentGridIndex;
    public GridTileEnumerator CurrentTileEnumerator;
    public bool TileEnumeratorValid;

    public readonly Queue<EntityUid> PendingEntities = new();
    public readonly HashSet<EntityUid> ProcessedEntities = new();
}

internal enum CEEffectProcessingPhase : byte
{
    Idle,
    Gathering,
    Applying,
}

/// <summary>
/// Raised on the weather entity for each exposed entity during the applying phase.
/// Subscribe on <see cref="Content.Shared._CE.Weather.CEWeatherEntityEffectComponent"/> to handle effect application.
/// </summary>
[ByRefEvent]
public record struct CEWeatherEntityAffectedEvent(EntityUid Target);
