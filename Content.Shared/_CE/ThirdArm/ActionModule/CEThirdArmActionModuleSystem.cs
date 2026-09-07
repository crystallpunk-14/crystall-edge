using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.ThirdArm.ActionModule;

public sealed partial class CEThirdArmActionModuleSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<CEThirdArmComponent> ent, ref GotEquippedEvent args)
    {
        var module = ent.Comp.ModuleSlot.Item;
        if (module != null && TryComp<CEThirdArmActionModuleComponent>(module, out var moduleComp))
        {
            foreach (var actionProto in moduleComp.Actions)
            {
                EntityUid? actionId = null;
                _actions.AddAction(args.EquipTarget, ref actionId, actionProto, module.Value);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<CEThirdArmComponent> ent, ref GotUnequippedEvent args)
    {
        var module = ent.Comp.ModuleSlot.Item;
        if (module != null)
            _actions.RemoveProvidedActions(args.EquipTarget, module.Value);
    }

    [SubscribeLocalEvent]
    private void OnModuleActivated(Entity<CEThirdArmActionModuleComponent> module, ref CEThirdArmModuleActivatedEvent args)
    {
        if (TryGetWearer(args.Arm, out var wearer))
        {
            foreach (var actionProto in module.Comp.Actions)
            {
                EntityUid? actionId = null;
                _actions.AddAction(wearer, ref actionId, actionProto, module);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnModuleDeactivated(Entity<CEThirdArmActionModuleComponent> module, ref CEThirdArmModuleDeactivatedEvent args)
    {
        if (TryGetWearer(args.Arm, out var wearer))
            _actions.RemoveProvidedActions(wearer, module);
    }

    private bool TryGetWearer(EntityUid arm, out EntityUid wearer)
    {
        wearer = default;

        if (!TryComp<ClothingComponent>(arm, out var clothing) || clothing.InSlot == null)
            return false;

        wearer = Transform(arm).ParentUid;
        return true;
    }
}
