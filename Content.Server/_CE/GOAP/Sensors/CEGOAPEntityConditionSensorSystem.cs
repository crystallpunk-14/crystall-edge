using Content.Shared._CE.GOAP.Components;
using Content.Shared.EntityConditions;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPEntityConditionSensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public EntityCondition Condition = default!;
}

/// <summary>
/// Publishes standard prototype-authored entity conditions as GOAP facts.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPEntityConditionSensorComponent : Component
{
    [DataField, AlwaysPushInheritance]
    public List<CEGOAPEntityConditionSensorEntry> Entries = new();

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextUpdateTime;
}

public sealed partial class CEGOAPEntityConditionSensorSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPEntityConditionSensorComponent, CEGOAPSensorRefreshEvent>(OnRefresh);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<
            CEGOAPEntityConditionSensorComponent,
            CEGOAPComponent,
            CEActiveGOAPComponent>();

        while (query.MoveNext(out var uid, out var sensor, out var goap, out _))
        {
            // A negative interval opts out of polling while explicit refresh events
            // remain available to authoritative state owners.
            if (sensor.UpdateInterval < TimeSpan.Zero || curTime < sensor.NextUpdateTime)
                continue;

            Evaluate(uid, sensor, goap);
            sensor.NextUpdateTime = curTime + sensor.UpdateInterval;
        }
    }

    private void OnRefresh(
        Entity<CEGOAPEntityConditionSensorComponent> ent,
        ref CEGOAPSensorRefreshEvent args)
    {
        if (TryComp<CEGOAPComponent>(ent, out var goap))
            Evaluate(ent, ent.Comp, goap);
    }

    private void Evaluate(
        EntityUid uid,
        CEGOAPEntityConditionSensorComponent sensor,
        CEGOAPComponent goap)
    {
        foreach (var entry in sensor.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ConditionKey))
                continue;

            goap.WorldState[entry.ConditionKey] = _conditions.TryCondition(uid, entry.Condition);
        }
    }
}
