using Content.Shared.Actions;

namespace Content.Shared._CE.NightVision;

public abstract class CESharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CENightVisionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CENightVisionComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<CENightVisionComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionPrototype);
    }

    protected virtual void OnRemove(Entity<CENightVisionComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }
}

public sealed partial class CEToggleNightVisionEvent : InstantActionEvent { }
