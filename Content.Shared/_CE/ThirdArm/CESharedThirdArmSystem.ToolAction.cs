using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Actions;
using Content.Shared.Interaction;

namespace Content.Shared._CE.ThirdArm;

/// <summary>
///     Generic mechanic for third arm modules that grant an action which uses the ACTION ENTITY ITSELF as a
///     tool on a targeted entity (via InteractUsing) - not the module. The action prototype carries its own
///     ToolComponent in yaml, so systems like ConstructionSystem's tool-quality checks (and their own DoAfter)
///     work normally, while the module stays impossible to use directly: it's never the "used" entity, and
///     action entities are never independently held or clicked by players.
/// </summary>
public abstract partial class CESharedThirdArmSystem
{
    [Dependency] protected SharedInteractionSystem Interaction = default!;

    private void InitToolAction()
    {
        SubscribeLocalEvent<CEThirdArmModuleComponent, CEThirdArmToolActionEvent>(OnToolAction);
    }

    private void OnToolAction(Entity<CEThirdArmModuleComponent> ent, ref CEThirdArmToolActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var clickCoordinates = Transform(args.Target).Coordinates;
        Interaction.InteractUsing(args.Performer, args.Action.Owner, args.Target, clickCoordinates);
    }
}

/// <summary>
///     Uses the action entity itself (which should carry a ToolComponent in yaml) as a tool on the targeted
///     entity, as if clicking it. Mana cost, if any, comes from a separate CEThirdArmActionManaCostComponent
///     on the action entity.
/// </summary>
public sealed partial class CEThirdArmToolActionEvent : EntityTargetActionEvent;
