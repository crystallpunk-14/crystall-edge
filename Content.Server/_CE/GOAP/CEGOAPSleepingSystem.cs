using System.Numerics;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Puts dormant GOAP agents to sleep while no player is near and wakes them on player proximity or damage.
/// Proximity is z-aware (see <see cref="CESharedZLevelsSystem.TryGetEffectiveDistance"/>) and scanned around players,
/// not agents, so the cost scales with the player count. Ghosts are not players here.
/// </summary>
public sealed partial class CEGOAPSleepingSystem : EntitySystem
{
    [Dependency] private CEGOAPSystem _goap = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;
    [Dependency] private EntityQuery<CEGOAPSleepingComponent> _sleepingQuery = default!;

    private bool _enabled = true;
    private float _wakeRadius;
    private float _sleepRadius;
    private float _zScale;
    private TimeSpan _interval;
    private TimeSpan _sleepDelay;

    private TimeSpan _nextCheck;

    private readonly HashSet<Entity<CEGOAPComponent>> _nearbyBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.CEGOAPDormancy, OnDormancyChanged, true);
        Subs.CVar(_cfg, CCVars.CEGOAPWakeRadius, v => _wakeRadius = v, true);
        Subs.CVar(_cfg, CCVars.CEGOAPSleepRadius, v => _sleepRadius = v, true);
        Subs.CVar(_cfg, CCVars.CEGOAPDormancyInterval, v => _interval = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, CCVars.CEGOAPSleepDelay, v => _sleepDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, CCVars.CEMurkZScale, v => _zScale = v, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        var now = _timing.CurTime;
        if (now < _nextCheck)
            return;

        _nextCheck = now + _interval;

        var playerQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (playerQuery.MoveNext(out var player, out _, out var xform))
        {
            if (_ghostQuery.HasComp(player) || xform.MapUid is not { } map)
                continue;

            ScanAroundPlayer(map, xform.MapID, _transform.GetWorldPosition(xform), now);
        }

        var agentQuery = EntityQueryEnumerator<CEActiveGOAPComponent, CEGOAPComponent>();
        while (agentQuery.MoveNext(out var uid, out _, out var goap))
        {
            if (goap.Dormant && now - goap.LastPlayerNearby >= _sleepDelay)
                PutToSleep((uid, goap));
        }
    }

    /// <summary>
    /// Called on agent MapInit: a dormant agent is born asleep unless a player is already within the wake radius.
    /// </summary>
    public void InitDormancy(Entity<CEGOAPComponent> ent)
    {
        if (!_enabled || !ent.Comp.Dormant)
            return;

        ent.Comp.LastPlayerNearby = _timing.CurTime;

        if (!IsPlayerNearby(ent, _wakeRadius))
            EnsureComp<CEGOAPSleepingComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<CEGOAPSleepingComponent> ent, ref DamageDealtEvent args)
    {
        WakeMob(ent);
    }

    /// <summary>
    /// Removes the sleeping marker and re-evaluates the GOAP awake status.
    /// </summary>
    public void WakeMob(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

        // Must use RemComp (not Deferred) so HasComp check in OnCheckAwake
        // sees the component as absent when UpdateAwakeStatus runs immediately after.
        RemComp<CEGOAPSleepingComponent>(uid);

        if (!TryComp<CEGOAPComponent>(uid, out var goap))
            return;

        // Fresh wake (e.g. from damage) shouldn't fall back asleep on the next check.
        goap.LastPlayerNearby = _timing.CurTime;
        _goap.UpdateAwakeStatus((uid, goap));
    }

    private void PutToSleep(Entity<CEGOAPComponent> ent)
    {
        EnsureComp<CEGOAPSleepingComponent>(ent);
        _goap.UpdateAwakeStatus((ent, ent.Comp));
    }

    /// <summary>
    /// Stamps dormant agents within the sleep radius as seen and wakes sleeping ones within the wake radius,
    /// on the player's map and every z-level the radius reaches.
    /// </summary>
    private void ScanAroundPlayer(EntityUid map, MapId mapId, Vector2 position, TimeSpan now)
    {
        var levelHeight = CESharedZLevelsSystem.ZLevelOffset * _zScale;
        var maxOffset = levelHeight > 0 ? (int) (_sleepRadius / levelHeight) : 0;

        for (var offset = -maxOffset; offset <= maxOffset; offset++)
        {
            var levelMapId = mapId;
            if (offset != 0)
            {
                // Missing levels can sit between existing ones, so skip rather than stop.
                if (!_zLevels.TryMapOffset((map, null), offset, out _, out var levelMap))
                    continue;

                levelMapId = levelMap.MapId;
            }

            var vertical = Math.Abs(offset) * levelHeight;
            var sleepPlanar = MathF.Sqrt(_sleepRadius * _sleepRadius - vertical * vertical);
            var wakePlanarSq = _wakeRadius * _wakeRadius - vertical * vertical;

            _nearbyBuffer.Clear();
            _lookup.GetEntitiesInRange(new MapCoordinates(position, levelMapId), sleepPlanar, _nearbyBuffer);

            foreach (var agent in _nearbyBuffer)
            {
                if (!agent.Comp.Dormant)
                    continue;

                agent.Comp.LastPlayerNearby = now;

                if (!_sleepingQuery.HasComp(agent) || wakePlanarSq < 0)
                    continue;

                var planarSq = (_transform.GetWorldPosition(agent) - position).LengthSquared();
                if (planarSq <= wakePlanarSq)
                    WakeMob(agent);
            }
        }
    }

    private bool IsPlayerNearby(EntityUid uid, float radius)
    {
        var playerQuery = EntityQueryEnumerator<ActorComponent>();
        while (playerQuery.MoveNext(out var player, out _))
        {
            if (_ghostQuery.HasComp(player))
                continue;

            if (_zLevels.TryGetEffectiveDistance(uid, player, out var distance) && distance <= radius)
                return true;
        }

        return false;
    }

    private void OnDormancyChanged(bool enabled)
    {
        _enabled = enabled;

        if (enabled)
            return;

        // Turning dormancy off releases everyone it put to sleep.
        var query = EntityQueryEnumerator<CEGOAPSleepingComponent>();
        var sleeping = new List<EntityUid>();
        while (query.MoveNext(out var uid, out _))
        {
            sleeping.Add(uid);
        }

        foreach (var uid in sleeping)
        {
            WakeMob(uid);
        }
    }
}
