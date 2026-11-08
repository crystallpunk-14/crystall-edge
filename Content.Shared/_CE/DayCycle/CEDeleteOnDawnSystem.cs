namespace Content.Shared._CE.DayCycle;

public sealed partial class CEDeleteOnDawnSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnStartDay(CEGlobalStartDayEvent args)
    {
        var query = EntityQueryEnumerator<CEDeleteOnDawnComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            PredictedQueueDel(uid);
        }
    }
}
