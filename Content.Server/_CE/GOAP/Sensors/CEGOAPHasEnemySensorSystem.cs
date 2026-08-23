using Content.Server._CE.GOAP.Classifiers;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPHasEnemySensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;
}

[RegisterComponent]
public sealed partial class CEGOAPHasEnemySensorComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public List<CEGOAPHasEnemySensorEntry> Entries = [];
}

public sealed class CEGOAPHasEnemySensorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGOAPHasEnemySensorComponent, CEGOAPKnowledgeCacheRebuiltEvent>(OnCacheRebuilt);
        SubscribeLocalEvent<CEGOAPHasEnemySensorComponent, CEGOAPSensorRefreshEvent>(OnRefresh);
    }

    private void OnCacheRebuilt(Entity<CEGOAPHasEnemySensorComponent> ent, ref CEGOAPKnowledgeCacheRebuiltEvent args)
    {
        EvaluateAll(ent);
    }

    private void OnRefresh(Entity<CEGOAPHasEnemySensorComponent> ent, ref CEGOAPSensorRefreshEvent args)
    {
        EvaluateAll(ent);
    }

    private void EvaluateAll(Entity<CEGOAPHasEnemySensorComponent> ent)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        if (!TryComp<CEGOAPKnowledgeCacheComponent>(ent, out var cache))
        {
            foreach (var entry in ent.Comp.Entries)
            {
                goap.WorldState[entry.ConditionKey] = false;
            }

            return;
        }

        var hasEnemy = cache.Enemies.Count > 0;
        foreach (var entry in ent.Comp.Entries)
        {
            goap.WorldState[entry.ConditionKey] = hasEnemy;
        }
    }
}
