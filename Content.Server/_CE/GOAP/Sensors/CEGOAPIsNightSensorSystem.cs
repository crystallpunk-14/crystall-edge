using Content.Shared._CE.DayCycle;
using Content.Shared._CE.GOAP.Selectors;

namespace Content.Server._CE.GOAP.Sensors;

public sealed partial class CEGOAPIsNightSensorEntry
    : CEGOAPSensorEntry<CEGOAPIsNightSensorEntry, CEGOAPIsNightSensorComponent>;

/// <summary>
/// Writes true while it is night on the agent's map. Maps without a day cycle (underground) count as night.
/// Event-driven: re-evaluated at dawn and dusk on the agent's map and when the agent moves to another map.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPIsNightSensorComponent : CEGOAPSensorComponent<CEGOAPIsNightSensorEntry>;

public sealed partial class CEGOAPIsNightSensorSystem
    : CEGOAPSensorSystem<CEGOAPIsNightSensorComponent, CEGOAPIsNightSensorEntry>
{
    [Dependency] private CEDayCycleSystem _dayCycle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnStartNight(CEStartNightEvent args)
    {
        EvaluateOnMap(args.MapUid);
    }

    [SubscribeLocalEvent]
    private void OnStartDay(CEStartDayEvent args)
    {
        EvaluateOnMap(args.MapUid);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<CEGOAPIsNightSensorComponent> ent, ref EntParentChangedMessage args)
    {
        if (args.OldMapId != args.Transform.MapUid)
            EvaluateAll(ent);
    }

    private void EvaluateOnMap(EntityUid map)
    {
        var query = EntityQueryEnumerator<CEGOAPIsNightSensorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var xform))
        {
            if (xform.MapUid == map)
                EvaluateAll((uid, sensor));
        }
    }

    protected override bool Evaluate(EntityUid agent, CEGOAPIsNightSensorEntry entry, CEGOAPSelectorResult target)
    {
        return _transform.GetMap(agent) is not { } map || !_dayCycle.IsDayNow(map);
    }
}
