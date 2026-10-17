using Content.Server.Polymorph.Systems;
using Content.Shared._CE.Polymorph;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._CE.ZLevels.Flight;

namespace Content.Server._CE.Polymorph;

public sealed partial class CERevertOnChasmSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    [SubscribeLocalEvent(after: [typeof(CESharedZFlightSystem)])]
    private void OnChasmAttempt(Entity<CERevertOnChasmComponent> ent, ref CEZLevelChasmAttempt args)
    {
        if (args.Cancelled)
            return;

        if (_polymorph.Revert((ent, null)) != null)
            args.Cancel();
    }
}
