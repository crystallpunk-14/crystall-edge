using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.MagicVision.Events;
using Content.Shared.Eye;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;

namespace Content.Server._CE.MagicVision;

/// <summary>
/// Grants entities magic vision (the <see cref="CEMagicVisionComponent"/> marker, which drives the
/// visibility mask and the client-side overlay) based on however many sources currently want them to
/// have it - clothing (<see cref="CEMagicVisionClothingComponent"/>), and potentially skills or other
/// sources in the future, all decided fresh each time via <see cref="CECheckMagicVisionEvent"/>. This
/// is entirely server-authoritative: the component is networked and drives PVS filtering, so the
/// client only ever mirrors it from replicated state and never predicts it.
/// </summary>
public sealed partial class CEMagicVisionSystem : EntitySystem
{
    [Dependency] private SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicVisionComponent, GetVisMaskEvent>(OnGetVisMask);

        SubscribeLocalEvent<CEMagicVisionClothingComponent, InventoryRelayedEvent<CECheckMagicVisionEvent>>(OnClothingCheckVision);
        SubscribeLocalEvent<CEMagicVisionClothingComponent, GotEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<CEMagicVisionClothingComponent, GotUnequippedEvent>(OnClothingUnequipped);
    }

    private void OnGetVisMask(Entity<CEMagicVisionComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.CEMagicVision;
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

    /// <summary>
    /// Recomputes whether <see cref="uid"/> currently has magic vision by asking every subscribed
    /// source via <see cref="CECheckMagicVisionEvent"/>, then adds or removes
    /// <see cref="CEMagicVisionComponent"/> to match. Sources (clothing, skills, etc.) should call
    /// this whenever their own contribution to magic vision might have changed - e.g. on equip/unequip
    /// or on skill learned/forgotten.
    /// </summary>
    /// <remarks>
    /// The visibility mask is refreshed here, right after the component add/remove completes, rather
    /// than from that component's own ComponentStartup/ComponentShutdown - during ComponentShutdown the
    /// component is still technically attached, so a GetVisMaskEvent raised from there would see it as
    /// present and re-add the flag it's supposed to be losing.
    /// </remarks>
    public void RefreshMagicVision(EntityUid uid)
    {
        var ev = new CECheckMagicVisionEvent();
        RaiseLocalEvent(uid, ev);

        if (ev.HasVision == HasComp<CEMagicVisionComponent>(uid))
            return;

        if (ev.HasVision)
            EnsureComp<CEMagicVisionComponent>(uid);
        else
            RemComp<CEMagicVisionComponent>(uid);

        _eye.RefreshVisibilityMask(uid);
    }
}
