using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPRangeToTargetSensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;

    /// <summary>
    /// Range threshold in tiles.
    /// </summary>
    [DataField(required: true)]
    public float Range = 1f;
}

/// <summary>
/// Checks if a selector-resolved target is within a specified range.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPRangeToTargetSensorComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public List<CEGOAPRangeToTargetSensorEntry> Entries = new();

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.2);

    [ViewVariables]
    public TimeSpan NextUpdateTime;
}

public sealed partial class CEGOAPRangeToTargetSensorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGOAPRangeToTargetSensorComponent, CEGOAPSensorRefreshEvent>(OnRefresh);
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CEGOAPRangeToTargetSensorComponent, CEGOAPComponent, CEActiveGOAPComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var goap, out _))
        {
            if (curTime < sensor.NextUpdateTime)
                continue;

            sensor.NextUpdateTime = curTime + sensor.UpdateInterval;
            foreach (var entry in sensor.Entries)
                EvaluateEntry(uid, entry, goap);
        }
    }

    private void OnRefresh(Entity<CEGOAPRangeToTargetSensorComponent> ent, ref CEGOAPSensorRefreshEvent args)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        foreach (var entry in ent.Comp.Entries)
            EvaluateEntry(ent, entry, goap);
    }

    private void EvaluateEntry(EntityUid uid, CEGOAPRangeToTargetSensorEntry entry, CEGOAPComponent goap)
    {
        var result = entry.Selector.Resolve(uid, EntityManager);

        if (!_xformQuery.TryGetComponent(uid, out var xform))
        {
            goap.WorldState[entry.ConditionKey] = false;
            return;
        }

        EntityCoordinates? targetCoords = null;
        if (result.Entity is { } e && _xformQuery.TryGetComponent(e, out var ex))
            targetCoords = ex.Coordinates;
        else if (result.Position is { } p)
            targetCoords = p;

        if (targetCoords is not { } coords ||
            !xform.Coordinates.TryDistance(EntityManager, coords, out var distance))
        {
            goap.WorldState[entry.ConditionKey] = false;
            return;
        }

        goap.WorldState[entry.ConditionKey] = distance <= entry.Range;
    }
}
