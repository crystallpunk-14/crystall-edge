using Content.Shared._CE.Roof;
using Content.Shared.Actions;

namespace Content.Server._CE.Roof;

public sealed class CERoofSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CERoofTogglerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CERoofTogglerComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<CERoofTogglerComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionProto);
    }

    private void OnRemove(Entity<CERoofTogglerComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }
}
