using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.ThirdArm;

public abstract partial class CESharedThirdArmSystem : EntitySystem
{
    [Dependency] protected ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedContainerSystem Container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThirdArmComponent, ComponentInit>(OnThirdArmInit);
        SubscribeLocalEvent<CEThirdArmComponent, ComponentShutdown>(OnThirdArmShutdown);
        SubscribeLocalEvent<CEThirdArmComponent, EntInsertedIntoContainerMessage>(OnModuleSlotChanged);
        SubscribeLocalEvent<CEThirdArmComponent, EntRemovedFromContainerMessage>(OnModuleSlotChanged);

        InitBattery();
    }

    private void OnThirdArmInit(Entity<CEThirdArmComponent> ent, ref ComponentInit args)
    {
        var container = Container.EnsureContainer<ContainerSlot>(ent, CEThirdArmComponent.ModuleSlotId);
        container.OccludesLight = false;

        ItemSlots.AddItemSlot(ent, CEThirdArmComponent.ModuleSlotId, ent.Comp.ModuleSlot);
    }

    private void OnThirdArmShutdown(Entity<CEThirdArmComponent> ent, ref ComponentShutdown args)
    {
        ItemSlots.RemoveItemSlot(ent, ent.Comp.ModuleSlot);
    }

    private void OnModuleSlotChanged(Entity<CEThirdArmComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CEThirdArmComponent.ModuleSlotId)
            return;

        UpdateAppearance(ent);

        var activatedEvent = new CEThirdArmModuleActivatedEvent(ent);
        RaiseLocalEvent(args.Entity, ref activatedEvent);

        UpdateModulePower(ent);
    }

    private void OnModuleSlotChanged(Entity<CEThirdArmComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CEThirdArmComponent.ModuleSlotId)
            return;

        UpdateAppearance(ent);

        // The module is already gone from the slot by this point, so refreshing the charge rate here
        // naturally drops its drain contribution.
        Battery.RefreshChargeRate(ent.Owner);
        SetModulePowered(args.Entity, false);

        var deactivatedEvent = new CEThirdArmModuleDeactivatedEvent(ent);
        RaiseLocalEvent(args.Entity, ref deactivatedEvent);
    }

    private void UpdateAppearance(Entity<CEThirdArmComponent> ent)
    {
        var module = ent.Comp.ModuleSlot.Item;

        if (module != null && TryComp<CEThirdArmModuleComponent>(module, out var moduleComp))
        {
            Appearance.SetData(ent, CEThirdArmVisuals.IconLayers, moduleComp.IconLayers);
            Appearance.SetData(ent, CEThirdArmVisuals.EquippedLayers, moduleComp.EquippedLayers);
        }
        else
        {
            Appearance.SetData(ent, CEThirdArmVisuals.IconLayers, new List<PrototypeLayerData>());
            Appearance.SetData(ent, CEThirdArmVisuals.EquippedLayers, new List<PrototypeLayerData>());
        }
    }
}

/// <summary>
///     Raised on a module entity when it is inserted into a third arm's module slot.
/// </summary>
[ByRefEvent]
public readonly record struct CEThirdArmModuleActivatedEvent(EntityUid Arm);

/// <summary>
///     Raised on a module entity when it is removed from a third arm's module slot.
/// </summary>
[ByRefEvent]
public readonly record struct CEThirdArmModuleDeactivatedEvent(EntityUid Arm);

[Serializable, NetSerializable]
public enum CEThirdArmVisuals : byte
{
    IconLayers,
    EquippedLayers
}
