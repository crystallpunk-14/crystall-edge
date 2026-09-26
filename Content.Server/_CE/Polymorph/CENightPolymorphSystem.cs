using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Polymorph;
using Content.Shared.Mobs.Systems;

namespace Content.Server._CE.Polymorph;

/// <summary>
/// Drives <see cref="CENightPolymorphComponent"/>: turns its holders into their configured
/// polymorph when night starts, and reverts every <see cref="CENightPolymorphedComponent"/> form
/// back at dawn. Death/crit reverts are already handled by <see cref="PolymorphSystem"/> itself.
/// </summary>
public sealed partial class CENightPolymorphSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnStartNight(CEGlobalStartNightEvent args)
    {
        var query = EntityQueryEnumerator<CENightPolymorphComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Someone already dead or downed when night falls stays as they are for the night.
            if (!_mobState.IsAlive(uid))
                continue;

            if (_polymorph.PolymorphEntity(uid, comp.Polymorph) is { } child)
                EnsureComp<CENightPolymorphedComponent>(child);
        }
    }

    [SubscribeLocalEvent]
    private void OnStartDay(CEGlobalStartDayEvent args)
    {
        var query = EntityQueryEnumerator<CENightPolymorphedComponent, PolymorphedEntityComponent>();
        while (query.MoveNext(out var uid, out _, out var polymorphed))
        {
            _polymorph.Revert((uid, polymorphed));
        }
    }
}
