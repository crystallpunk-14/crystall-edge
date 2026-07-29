using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.MagicVision.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._CE.MagicVision;

public abstract partial class CESharedMagicVisionSystem
{
    private void InitializeClothing()
    {
        SubscribeLocalEvent<CEMagicVisionClothingComponent, InventoryRelayedEvent<CECheckMagicVisionEvent>>(OnClothingCheckVision);
        SubscribeLocalEvent<CEMagicVisionClothingComponent, GotEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<CEMagicVisionClothingComponent, GotUnequippedEvent>(OnClothingUnequipped);
    }

    private void OnClothingCheckVision(Entity<CEMagicVisionClothingComponent> ent, ref InventoryRelayedEvent<CECheckMagicVisionEvent> args)
    {
        args.Args.GrantVision();
    }

    private void OnClothingEquipped(Entity<CEMagicVisionClothingComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.EYES) == 0)
            return;

        RefreshMagicVision(args.EquipTarget);
    }

    private void OnClothingUnequipped(Entity<CEMagicVisionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.EYES) == 0)
            return;

        RefreshMagicVision(args.EquipTarget);
    }
}
