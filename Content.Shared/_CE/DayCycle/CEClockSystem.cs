using Content.Shared.Examine;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.DayCycle;

public sealed partial class CEClockSystem : EntitySystem
{
    [Dependency] private CEDayCycleSystem _dayCycle = default!;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CEClockComponent> ent, ref ExaminedEvent args)
    {
        if (Transform(ent).MapUid is not { } map)
            return;

        if (_dayCycle.TryGetTimeUntilTransitionText(map, out var text))
            args.PushText(text);
    }
}
