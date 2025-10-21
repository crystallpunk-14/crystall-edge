using Content.Shared._CE.ZLevel;
using Content.Shared.Actions;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEStationZLevelsSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    private void InitActions()
    {
        SubscribeLocalEvent<CEZLevelMoverComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEZLevelMoverComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CEZLevelMoverComponent, CEZLevelActionUp>(OnZLevelUp);
        SubscribeLocalEvent<CEZLevelMoverComponent, CEZLevelActionDown>(OnZLevelDown);
    }

    private void OnMapInit(Entity<CEZLevelMoverComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ZLevelUpActionEntity, ent.Comp.UpActionProto);
        _actions.AddAction(ent, ref ent.Comp.ZLevelDownActionEntity, ent.Comp.DownActionProto);
    }

    private void OnRemove(Entity<CEZLevelMoverComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.ZLevelUpActionEntity);
        _actions.RemoveAction(ent.Comp.ZLevelDownActionEntity);
    }

    private void OnZLevelDown(Entity<CEZLevelMoverComponent> ent, ref CEZLevelActionDown args)
    {
        if (args.Handled)
            return;

        args.Handled = TryMoveDown(ent);
    }

    private void OnZLevelUp(Entity<CEZLevelMoverComponent> ent, ref CEZLevelActionUp args)
    {
        if (args.Handled)
            return;

        args.Handled = TryMoveUp(ent);
    }
}
