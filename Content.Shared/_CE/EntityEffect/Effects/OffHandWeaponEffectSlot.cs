using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.MeleeWeapon;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Reads effects from a named slot on the off-hand weapon and executes them with the off-hand weapon as Used.
/// Mirrors <see cref="WeaponEffectSlot"/> but resolves the weapon from the non-active hand at effect-time.
/// </summary>
public sealed partial class OffHandWeaponEffectSlot : CEEntityEffectBase<OffHandWeaponEffectSlot>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField]
    public float Power = 1f;
}

public sealed partial class CEOffHandWeaponEffectSlotSystem : CEEntityEffectSystem<OffHandWeaponEffectSlot>
{
    [Dependency] private SharedHandsSystem _hands = default!;

    [Dependency] private EntityQuery<CEWeaponComponent> _weaponQuery = default!;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<OffHandWeaponEffectSlot> args)
    {
        if (args.Args.Used is null)
            return;
        if (!_handsQuery.TryComp(args.Args.Source, out var hands))
            return;
        if (hands.ActiveHandId is null)
            return;

        // Verify main weapon is in the active hand (prevents firing outside dual-wield context)
        if (!_hands.TryGetHeldItem(args.Args.Source, hands.ActiveHandId, out var activeItem)
            || activeItem != args.Args.Used.Value)
            return;

        // Find the first weapon in any non-active hand
        EntityUid? offHandWeapon = null;
        foreach (var handId in hands.SortedHands)
        {
            if (handId == hands.ActiveHandId)
                continue;
            if (!_hands.TryGetHeldItem(args.Args.Source, handId, out var held))
                continue;
            if (!_weaponQuery.HasComp(held.Value))
                continue;
            offHandWeapon = held.Value;
            break;
        }

        if (offHandWeapon is null)
            return;
        if (!_weaponQuery.TryComp(offHandWeapon.Value, out var offHandComp))
            return;

        var slots = offHandComp.EffectSlots;
        if (!slots.TryGetValue(args.Effect.Slot, out var effects))
            return;

        var offHandArgs = args.Args with { Used = offHandWeapon.Value, Power = args.Args.Power * args.Effect.Power };
        foreach (var effect in effects)
        {
            effect.Effect(offHandArgs);
        }
    }
}
