using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Polymorph;

namespace Content.Server._CE.Polymorph;

/// <summary>
/// Reverts every <see cref="CEUnpolymorphOnDawnComponent"/> form back to normal at dawn - baked
/// directly into whichever polymorph target prototype should always undo itself once night ends,
/// regardless of what triggered the transformation in the first place. Death/crit reverts are
/// already handled by <see cref="PolymorphSystem"/> itself.
/// </summary>
public sealed partial class CENightPolymorphSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    [SubscribeLocalEvent]
    private void OnStartDay(CEGlobalStartDayEvent args)
    {
        var query = EntityQueryEnumerator<CEUnpolymorphOnDawnComponent, PolymorphedEntityComponent>();
        while (query.MoveNext(out var uid, out _, out var polymorphed))
        {
            _polymorph.Revert((uid, polymorphed));
        }
    }
}
