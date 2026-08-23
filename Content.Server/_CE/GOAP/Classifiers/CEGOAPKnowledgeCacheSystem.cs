using Content.Shared._CE.GOAP.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server._CE.GOAP.Classifiers;

/// <summary>
/// Maintains <see cref="CEGOAPKnowledgeCacheComponent"/> by reacting to
/// <see cref="CEGOAPKnowledgeUpdatedEvent"/>. Splits known entities into enemies/allies
/// using <see cref="NpcFactionSystem"/>. Also maintains <see cref="CEGOAPTargetComponent"/>
/// on classified enemies and raises <see cref="CEGOAPEnemyAcquiredEvent"/> when an agent
/// transitions from having no known enemies to having at least one.
/// </summary>
public sealed partial class CEGOAPKnowledgeCacheSystem : EntitySystem
{
    [Dependency] private NpcFactionSystem _faction = default!;

    [Dependency] private EntityQuery<NpcFactionMemberComponent> _factionQuery = default!;

    private readonly HashSet<EntityUid> _previousEnemies = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGOAPKnowledgeCacheComponent, CEGOAPKnowledgeUpdatedEvent>(OnKnowledgeUpdated);
        SubscribeLocalEvent<CEGOAPKnowledgeCacheComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnKnowledgeUpdated(Entity<CEGOAPKnowledgeCacheComponent> ent, ref CEGOAPKnowledgeUpdatedEvent args)
    {
        Rebuild(ent);
    }

    private void OnShutdown(Entity<CEGOAPKnowledgeCacheComponent> ent, ref ComponentShutdown args)
    {
        foreach (var enemy in ent.Comp.Enemies)
        {
            UntrackEnemy(enemy, ent.Owner);
        }

        ent.Comp.Enemies.Clear();
        ent.Comp.Allies.Clear();
    }

    private void Rebuild(Entity<CEGOAPKnowledgeCacheComponent> ent)
    {
        var (uid, cache) = ent;

        _previousEnemies.Clear();
        foreach (var prev in cache.Enemies)
        {
            _previousEnemies.Add(prev);
        }

        cache.Enemies.Clear();
        cache.Allies.Clear();

        if (!TryComp<CEGOAPComponent>(uid, out var goap))
        {
            foreach (var prev in _previousEnemies)
            {
                UntrackEnemy(prev, uid);
            }

            return;
        }

        _factionQuery.TryGetComponent(uid, out var selfFaction);

        foreach (var (target, _) in goap.Knowledge)
        {
            if (target == uid)
                continue;

            if (!_factionQuery.TryGetComponent(target, out var targetFaction))
                continue;

            if (selfFaction != null && _faction.IsEntityFriendly((uid, selfFaction), (target, targetFaction)))
            {
                cache.Allies.Add(target);
                continue;
            }

            if (selfFaction != null && selfFaction.HostileFactions.Overlaps(targetFaction.Factions))
                cache.Enemies.Add(target);
        }

        foreach (var prev in _previousEnemies)
        {
            if (!cache.Enemies.Contains(prev))
                UntrackEnemy(prev, uid);
        }

        foreach (var current in cache.Enemies)
        {
            if (!_previousEnemies.Contains(current))
                TrackEnemy(current, uid);
        }

        if (_previousEnemies.Count == 0 && cache.Enemies.Count > 0)
        {
            EntityUid? sample = null;
            foreach (var first in cache.Enemies)
            {
                sample = first;
                break;
            }
            var ev = new CEGOAPEnemyAcquiredEvent(sample);
            RaiseLocalEvent(uid, ref ev);
        }

        var rebuilt = new CEGOAPKnowledgeCacheRebuiltEvent();
        RaiseLocalEvent(uid, ref rebuilt);
    }

    private void TrackEnemy(EntityUid enemy, EntityUid agent)
    {
        var comp = EnsureComp<CEGOAPTargetComponent>(enemy);
        comp.Trackers.Add(agent);
    }

    private void UntrackEnemy(EntityUid enemy, EntityUid agent)
    {
        if (!TryComp<CEGOAPTargetComponent>(enemy, out var comp))
            return;

        comp.Trackers.Remove(agent);
        if (comp.Trackers.Count == 0)
            RemCompDeferred<CEGOAPTargetComponent>(enemy);
    }
}

/// <summary>
/// Raised on a GOAP agent the first tick its <see cref="CEGOAPKnowledgeCacheComponent.Enemies"/>
/// set transitions from empty to non-empty.
/// </summary>
[ByRefEvent]
public record struct CEGOAPEnemyAcquiredEvent(EntityUid? FirstEnemy);

/// <summary>
/// Raised on a GOAP agent at the end of every <see cref="CEGOAPKnowledgeCacheSystem"/> rebuild,
/// after the cache buckets are up-to-date. Consumers that read the cache (e.g. sensors writing
/// world-state from cache contents) should subscribe to this rather than
/// <see cref="CEGOAPKnowledgeUpdatedEvent"/> to avoid ordering issues.
/// </summary>
[ByRefEvent]
public record struct CEGOAPKnowledgeCacheRebuiltEvent;
