using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPRangeToTargetSensorEntry
    : CEGOAPSensorEntry<CEGOAPRangeToTargetSensorEntry, CEGOAPRangeToTargetSensorComponent>
{
    /// <summary>
    /// Range threshold in tiles.
    /// </summary>
    [DataField(required: true)]
    public float Range = 1f;
}

/// <summary>
/// Checks if the target slot is within a specified range. Polled while the agent is active.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPRangeToTargetSensorComponent : CEGOAPSensorComponent<CEGOAPRangeToTargetSensorEntry>
{
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.2);

    [ViewVariables]
    public TimeSpan NextUpdateTime;
}

public sealed partial class CEGOAPRangeToTargetSensorSystem
    : CEGOAPSensorSystem<CEGOAPRangeToTargetSensorComponent, CEGOAPRangeToTargetSensorEntry>
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CEGOAPRangeToTargetSensorComponent, CEGOAPComponent, CEActiveGOAPComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var goap, out _))
        {
            if (curTime < sensor.NextUpdateTime)
                continue;

            sensor.NextUpdateTime = curTime + sensor.UpdateInterval;
            EvaluateAll((uid, sensor), goap);
        }
    }

    protected override bool Evaluate(EntityUid agent, CEGOAPRangeToTargetSensorEntry entry, CEGOAPSelectorResult target)
    {
        if (!_xformQuery.TryGetComponent(agent, out var xform))
            return false;

        EntityCoordinates? targetCoords = null;
        if (target.Entity is { } entity && _xformQuery.TryGetComponent(entity, out var targetXform))
            targetCoords = targetXform.Coordinates;
        else if (target.Position is { } position)
            targetCoords = position;

        return targetCoords is { } coords
               && xform.Coordinates.TryDistance(EntityManager, coords, out var distance)
               && distance <= entry.Range;
    }
}
