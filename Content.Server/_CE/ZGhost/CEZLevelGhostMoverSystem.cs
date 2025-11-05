using Content.Shared._CE.ZGhost;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Actions;

namespace Content.Server._CE.ZGhost;

public sealed class CEZLevelGhostMoverSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelGhostMoverComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, CEZLevelActionUp>(OnZLevelUp);
        SubscribeLocalEvent<CEZLevelGhostMoverComponent, CEZLevelActionDown>(OnZLevelDown);

        SubscribeLocalEvent<CEZLevelViewerComponent, MapInitEvent>(OnViewerMapInit);
        SubscribeLocalEvent<CEZLevelViewerComponent, ComponentRemove>(OnViewerRemove);
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

    private void OnViewerMapInit(Entity<CEZLevelViewerComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ZLevelActionEntity, ent.Comp.ActionProto);
    }

    private void OnViewerRemove(Entity<CEZLevelViewerComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.ZLevelActionEntity);
    }

    private void OnZLevelDown(Entity<CEZLevelGhostMoverComponent> ent, ref CEZLevelActionDown args)
    {
        if (args.Handled)
            return;

        args.Handled = _zLevel.TryMoveDown(ent);
    }

    private void OnZLevelUp(Entity<CEZLevelGhostMoverComponent> ent, ref CEZLevelActionUp args)
    {
        if (args.Handled)
            return;

        args.Handled = _zLevel.TryMoveUp(ent);
    }
}
