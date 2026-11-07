using Content.Shared._CE.DayCycle;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._CE.StatusEffect.RemoveOnDawn;

public sealed partial class CERemoveOnDawnStatusEffectSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnStartDay(CEGlobalStartDayEvent args)
    {
        var query = EntityQueryEnumerator<CERemoveOnDawnStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            PredictedQueueDel(uid);
        }
    }
}
