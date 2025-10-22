using Content.Shared._CE.ZLevels;
using Content.Shared.Actions;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    private void InitActions()
    {
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, CEZLevelActionUp>(OnZLevelUp);
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, CEZLevelActionDown>(OnZLevelDown);
    }

    private void OnMapInit(Entity<CEZLevelGhostMoverComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ZLevelUpActionEntity, ent.Comp.UpActionProto);
        _actions.AddAction(ent, ref ent.Comp.ZLevelDownActionEntity, ent.Comp.DownActionProto);
    }

    private void OnRemove(Entity<CEZLevelGhostMoverComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.ZLevelUpActionEntity);
        _actions.RemoveAction(ent.Comp.ZLevelDownActionEntity);
    }

    private void OnZLevelDown(Entity<CEZLevelGhostMoverComponent> ent, ref CEZLevelActionDown args)
    {
        if (args.Handled)
            return;

        args.Handled = TryMoveDown(ent);
    }

    private void OnZLevelUp(Entity<CEZLevelGhostMoverComponent> ent, ref CEZLevelActionUp args)
    {
        if (args.Handled)
            return;

        args.Handled = TryMoveUp(ent);
    }
}
