using Content.Shared.Actions;
using Content.Shared.Throwing;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    private void InitView()
    {
        SubscribeLocalEvent<CEZLevelViewerComponent, MoveEvent>(OnViewerMove);
        SubscribeLocalEvent<CEZLevelViewerComponent, CEToggleZLevelLookUpAction>(OnToggleLookUp);
        SubscribeLocalEvent<CEZLevelViewerComponent, ThrowEvent>(OnThrow);
    }

    /// <summary>
    /// If you look up and throw something, you will throw it up by 1 z-level.
    /// </summary>
    private void OnThrow(Entity<CEZLevelViewerComponent> ent, ref ThrowEvent args)
    {
        if (!ent.Comp.LookUp)
            return;

        if (!TryComp<CEZPhysicsComponent>(args.Thrown, out var thrownZPhys))
            return;

        thrownZPhys.Velocity += ent.Comp.ThrowUpForce;
        DirtyField(args.Thrown, thrownZPhys, nameof(CEZPhysicsComponent.Velocity));
    }

    protected virtual void OnViewerMove(Entity<CEZLevelViewerComponent> ent, ref MoveEvent args)
    {
        if (!ent.Comp.LookUp)
            return;

        if (!HasRoof(ent))
            return;

        ent.Comp.LookUp = false;
        DirtyField(ent, ent.Comp, nameof(CEZLevelViewerComponent.LookUp));
    }

    private void OnToggleLookUp(Entity<CEZLevelViewerComponent> ent, ref CEToggleZLevelLookUpAction args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (HasRoof(ent))
        {
            Popup.PopupClient(Loc.GetString("ce-zlevel-look-up-fail"), ent, ent);
            return;
        }

        ent.Comp.LookUp = !ent.Comp.LookUp;
        DirtyField(ent, ent.Comp, nameof(CEZLevelViewerComponent.LookUp));
    }
}

public sealed partial class CEToggleZLevelLookUpAction : InstantActionEvent
{
}
