using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.MeleeWeapon.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;

namespace Content.Shared._CE.MeleeWeapon;

public sealed partial class CEDualWieldSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TagSystem _tag = default!;

    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;
    [Dependency] private EntityQuery<CEWeaponComponent> _weaponQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEDualWieldComponent, CEGetWeaponAnimationsEvent>(OnGetAnimations);
        SubscribeLocalEvent<CEDualWieldComponent, CEWeaponUseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<CEDualWieldComponent, CEWeaponUsedEvent>(OnUsed);
    }

    private void OnGetAnimations(Entity<CEDualWieldComponent> ent, ref CEGetWeaponAnimationsEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetOffHandWeapon(ent.Owner, args.User, out var offHandWeapon))
            return;

        foreach (var animSet in ent.Comp.AnimationSets)
        {
            if (animSet.UseType != args.UseType)
                continue;
            if (!_tag.HasTag(offHandWeapon.Value, animSet.Tag))
                continue;

            args.Animations = animSet.Animations;
            args.Handled = true;
            return;
        }
    }

    private void OnUseAttempt(Entity<CEDualWieldComponent> ent, ref CEWeaponUseAttemptEvent args)
    {
        if (!TryGetOffHandWeapon(ent.Owner, args.User, out var offHandWeapon))
            return;

        if (!HasMatchingAnimSet(ent.Comp, offHandWeapon.Value, args.UseType))
            return;

        var offHandAttempt = new CEWeaponUseAttemptEvent(args.User, args.UseType);
        RaiseLocalEvent(offHandWeapon.Value, offHandAttempt);

        if (offHandAttempt.Cancelled)
            args.Cancel();
    }

    private void OnUsed(Entity<CEDualWieldComponent> ent, ref CEWeaponUsedEvent args)
    {
        if (!TryGetOffHandWeapon(ent.Owner, args.User, out var offHandWeapon))
            return;

        if (!HasMatchingAnimSet(ent.Comp, offHandWeapon.Value, args.UseType))
            return;

        var offHandUsed = new CEWeaponUsedEvent(args.User, args.UseType);
        RaiseLocalEvent(offHandWeapon.Value, offHandUsed);
    }

    /// <summary>
    /// Finds the first weapon in any non-active hand of the user, but only if mainWeapon is in the active hand.
    /// The active-hand check acts as a recursion guard when both weapons have CEDualWieldComponent.
    /// </summary>
    private bool TryGetOffHandWeapon(EntityUid mainWeapon, EntityUid user, [NotNullWhen(true)] out EntityUid? offHandWeapon)
    {
        offHandWeapon = null;

        if (!_handsQuery.TryComp(user, out var hands))
            return false;
        if (hands.ActiveHandId is null)
            return false;

        if (!_hands.TryGetHeldItem(user, hands.ActiveHandId, out var activeItem) || activeItem != mainWeapon)
            return false;

        foreach (var handId in hands.SortedHands)
        {
            if (handId == hands.ActiveHandId)
                continue;
            if (!_hands.TryGetHeldItem(user, handId, out var held))
                continue;
            if (!_weaponQuery.HasComp(held.Value))
                continue;

            offHandWeapon = held.Value;
            return true;
        }

        return false;
    }

    private bool HasMatchingAnimSet(CEDualWieldComponent comp, EntityUid offHandWeapon, CEUseType useType)
    {
        foreach (var animSet in comp.AnimationSets)
        {
            if (animSet.UseType == useType && _tag.HasTag(offHandWeapon, animSet.Tag))
                return true;
        }
        return false;
    }
}
