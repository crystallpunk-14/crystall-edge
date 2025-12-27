using Content.Server.Actions;
using Content.Shared._CE.ZLevels.Flight;
using Content.Shared._CE.ZLevels.Flight.Components;

namespace Content.Server._CE.ZLevels.Flight;

public sealed class CEZFlightSystem : CESharedZFlightSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZFlyerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEZFlyerComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(Entity<CEZFlyerComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.ZLevelUpActionEntity);
        _actions.RemoveAction(ent.Comp.ZLevelDownActionEntity);
        _actions.RemoveAction(ent.Comp.ZLevelToggleActionEntity);
    }

    private void OnMapInit(Entity<CEZFlyerComponent> ent, ref MapInitEvent args)
    {
        if (!ZPhyzQuery.TryComp(ent, out var zPhys))
            return;

        SetTargetHeight(ent, zPhys.CurrentZLevel);

        _actions.AddAction(ent, ref ent.Comp.ZLevelUpActionEntity, ent.Comp.UpActionProto);
        _actions.AddAction(ent, ref ent.Comp.ZLevelDownActionEntity, ent.Comp.DownActionProto);
        _actions.AddAction(ent, ref ent.Comp.ZLevelToggleActionEntity, ent.Comp.ToggleActionProto);

        _actions.SetEnabled(ent.Comp.ZLevelDownActionEntity, ent.Comp.Active);
        _actions.SetEnabled(ent.Comp.ZLevelUpActionEntity, ent.Comp.Active);
    }
}
