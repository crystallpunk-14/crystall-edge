using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared._CE.ThirdArm;

public abstract partial class CESharedThirdArmSystem
{
    [Dependency] protected SharedActionsSystem Actions = default!;

    private void InitActions()
    {
        SubscribeLocalEvent<CEThirdArmComponent, GotEquippedEvent>(OnArmEquipped);
        SubscribeLocalEvent<CEThirdArmComponent, GotUnequippedEvent>(OnArmUnequipped);
        SubscribeLocalEvent<CEThirdArmModuleActionsGrantComponent, CEThirdArmModuleActivatedEvent>(OnActionModuleActivated);
        SubscribeLocalEvent<CEThirdArmModuleActionsGrantComponent, CEThirdArmModuleDeactivatedEvent>(OnActionModuleDeactivated);
    }

    private void OnArmEquipped(Entity<CEThirdArmComponent> ent, ref GotEquippedEvent args)
    {
        var module = ent.Comp.ModuleSlot.Item;
        if (module != null)
            GrantModuleActions(args.EquipTarget, module.Value);
    }

    private void OnArmUnequipped(Entity<CEThirdArmComponent> ent, ref GotUnequippedEvent args)
    {
        var module = ent.Comp.ModuleSlot.Item;
        if (module != null)
            RevokeModuleActions(args.EquipTarget, module.Value);
    }

    private void OnActionModuleActivated(Entity<CEThirdArmModuleActionsGrantComponent> ent, ref CEThirdArmModuleActivatedEvent args)
    {
        if (TryGetWearer(args.Arm, out var wearer))
            GrantModuleActions(wearer, ent);
    }

    private void OnActionModuleDeactivated(Entity<CEThirdArmModuleActionsGrantComponent> ent, ref CEThirdArmModuleDeactivatedEvent args)
    {
        if (TryGetWearer(args.Arm, out var wearer))
            RevokeModuleActions(wearer, ent);
    }

    private bool TryGetWearer(EntityUid arm, out EntityUid wearer)
    {
        wearer = default;

        if (!TryComp<ClothingComponent>(arm, out var clothing) || clothing.InSlot == null)
            return false;

        wearer = Transform(arm).ParentUid;
        return true;
    }

    private void GrantModuleActions(EntityUid wearer, EntityUid module)
    {
        if (!TryComp<CEThirdArmModuleActionsGrantComponent>(module, out var grant))
            return;

        foreach (var actionProto in grant.Actions)
        {
            EntityUid? actionId = null;
            Actions.AddAction(wearer, ref actionId, actionProto, module);
        }
    }

    private void RevokeModuleActions(EntityUid wearer, EntityUid module)
    {
        if (!HasComp<CEThirdArmModuleActionsGrantComponent>(module))
            return;

        Actions.RemoveProvidedActions(wearer, module);
    }
}
