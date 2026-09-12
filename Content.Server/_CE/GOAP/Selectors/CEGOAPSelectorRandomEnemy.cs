using Content.Server._CE.GOAP.Classifiers;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Picks a random known enemy from <see cref="CEGOAPKnowledgeCacheComponent.Enemies"/>.
/// </summary>
public sealed partial class CEGOAPSelectorRandomEnemy : CEGOAPTargetSelectorBase<CEGOAPSelectorRandomEnemy>
{
}

public sealed partial class CEGOAPSelectorRandomEnemySystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorRandomEnemy>
{
    [Dependency] private IRobustRandom _random = default!;
    // CrystallEdge: Rogue used CEMobStateSystem (CE-only). This fork has no CE health stack,
    // so use vanilla MobStateSystem instead.
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<CEGOAPKnowledgeCacheComponent> _cacheQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorRandomEnemy> ev)
    {
        if (!_cacheQuery.TryGetComponent(ev.Agent, out var cache) || cache.Enemies.Count == 0)
            return;

        var aliveEnemies = new List<EntityUid>();
        foreach (var enemy in cache.Enemies)
        {
            var isAlive = _mobStateQuery.TryGetComponent(enemy, out var mobState)
                ? !_mobState.IsIncapacitated(enemy, mobState)
                : !Terminating(enemy);
            if (isAlive)
                aliveEnemies.Add(enemy);
        }

        if (aliveEnemies.Count == 0)
            return;

        var chosen = _random.Pick(aliveEnemies);
        ev.Entity = chosen;
        if (_xformQuery.TryGetComponent(chosen, out var xform))
            ev.Position = xform.Coordinates;
    }
}
