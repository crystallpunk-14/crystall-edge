using Content.Shared._CE.ZLevel;
using Content.Shared.Actions;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEStationZLevelsSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    private void InitActions()
    {
        SubscribeLocalEvent<CEZLevelMoverComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEZLevelMoverComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CEZLevelMoverComponent, CEZLevelActionUp>(OnZLevelUpGhost);
        SubscribeLocalEvent<CEZLevelMoverComponent, CEZLevelActionDown>(OnZLevelDownGhost);
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

    private void OnZLevelDownGhost(Entity<CEZLevelMoverComponent> ent, ref CEZLevelActionDown args)
    {
        if (args.Handled)
            return;

        ZLevelMove(ent, -1);

        args.Handled = true;
    }

    private void OnZLevelUpGhost(Entity<CEZLevelMoverComponent> ent, ref CEZLevelActionUp args)
    {
        if (args.Handled)
            return;

        ZLevelMove(ent, 1);

        args.Handled = true;
    }

    private void ZLevelMove(EntityUid ent, int offset)
    {
        var xform = Transform(ent);
        var map = xform.MapUid;

        if (map is null)
            return;

        var targetMap = GetMapOffset(map.Value, offset);

        if (targetMap is null)
            return;

        _transform.SetMapCoordinates(ent, new MapCoordinates(_transform.GetWorldPosition(ent), targetMap.Value));
    }
}
