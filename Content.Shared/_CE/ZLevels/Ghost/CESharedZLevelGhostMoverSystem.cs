/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */


using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.ZLevels.Ghost;

public abstract partial class CESharedZLevelGhostMoverSystem : EntitySystem
{
    [Dependency] private CESharedZLevelsSystem _zLevel = null!;

    [SubscribeLocalEvent]
    private void OnZLevelDown(Entity<CEZLevelGhostMoverComponent> ent, ref CEZLevelActionDown args)
    {
        if (args.Handled)
            return;

        args.Handled = _zLevel.TryMoveDown(ent);
    }

    [SubscribeLocalEvent]
    private void OnZLevelUp(Entity<CEZLevelGhostMoverComponent> ent, ref CEZLevelActionUp args)
    {
        if (args.Handled)
            return;

        args.Handled = _zLevel.TryMoveUp(ent);
    }
}
