using System.Numerics;
using Content.Server._CE.GOAP.Classifiers;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Picks the spatially nearest known enemy from
/// <see cref="CEGOAPKnowledgeCacheComponent.Enemies"/>. Returns the enemy entity and its
/// current coordinates when alive; otherwise falls back to the remembered
/// <see cref="CEGOAPKnowledgeEntry.LastSeenCoords"/> for that entry.
/// </summary>
public sealed partial class CEGOAPSelectorNearestEnemy : CEGOAPTargetSelectorBase<CEGOAPSelectorNearestEnemy>
{
}

public sealed partial class CEGOAPSelectorNearestEnemySystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorNearestEnemy>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    // CrystallEdge: Rogue used CEMobStateSystem (CE-only). This fork has no CE health stack,
    // so use vanilla MobStateSystem instead.
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<CEGOAPKnowledgeCacheComponent> _cacheQuery = default!;
    [Dependency] private EntityQuery<CEGOAPComponent> _goapQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorNearestEnemy> ev)
    {
        if (!_cacheQuery.TryGetComponent(ev.Agent, out var cache) || cache.Enemies.Count == 0)
            return;

        if (!_xformQuery.TryGetComponent(ev.Agent, out var selfXform))
            return;

        _goapQuery.TryGetComponent(ev.Agent, out var goap);

        var selfPos = _transform.GetWorldPosition(selfXform);
        EntityUid? bestAlive = null;
        var bestAliveDist = float.MaxValue;

        foreach (var enemy in cache.Enemies)
        {
            if (_mobStateQuery.TryGetComponent(enemy, out var mobState)
                ? _mobState.IsIncapacitated(enemy, mobState)
                : Terminating(enemy))
                continue;

            if (!_xformQuery.TryGetComponent(enemy, out var ex))
                continue;

            var d = Vector2.Distance(selfPos, _transform.GetWorldPosition(ex));
            if (d < bestAliveDist)
            {
                bestAliveDist = d;
                bestAlive = enemy;
            }
        }

        if (bestAlive is not null && _xformQuery.TryGetComponent(bestAlive.Value, out var bestAliveXform))
        {
            ev.Entity = bestAlive.Value;
            ev.Position = bestAliveXform.Coordinates;
            return;
        }

        if (goap == null)
            return;

        EntityCoordinates? rememberedCoords = null;
        var rememberedDist = float.MaxValue;
        foreach (var enemy in cache.Enemies)
        {
            if (!goap.Knowledge.TryGetValue(enemy, out var entry))
                continue;

            if (!entry.LastSeenCoords.TryDistance(EntityManager, selfXform.Coordinates, out var distance))
                continue;

            if (distance < rememberedDist)
            {
                rememberedDist = distance;
                rememberedCoords = entry.LastSeenCoords;
            }
        }

        if (rememberedCoords != null)
            ev.Position = rememberedCoords;
    }
}
