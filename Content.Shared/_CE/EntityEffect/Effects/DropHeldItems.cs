using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// The target loses what it holds in its hands: the items are thrown in random directions,
/// so fragile ones may break on landing.
/// </summary>
public sealed partial class DropHeldItems : CEEntityEffectBase<DropHeldItems>
{
    [DataField]
    public float ThrowPower = 5f;

    [DataField]
    public float Distance = 1.5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-drop-held-items");
}

public sealed partial class CEDropHeldItemsEffectSystem : CEEntityEffectSystem<DropHeldItems>
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<DropHeldItems> args)
    {
        // Random throw directions can't be predicted.
        if (!_net.IsServer)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        var dropped = false;
        foreach (var held in _hands.EnumerateHeld(target))
        {
            // Virtual items (pulling, two-handed grip) are released silently, there is nothing to throw.
            var isVirtual = HasComp<VirtualItemComponent>(held);

            if (!_hands.TryDrop(target, held, checkActionBlocker: false) || isVirtual)
                continue;

            dropped = true;

            // Inside a locker or similar the item stays in that container.
            if (_container.IsEntityInContainer(held))
                continue;

            // The target is passed as the thrower so the item doesn't hit its own owner.
            var direction = _random.NextAngle().ToVec() * args.Effect.Distance;
            _throwing.TryThrow(held, direction, args.Effect.ThrowPower, target, pushbackRatio: 0f, recoil: false);
        }

        if (dropped)
            _popup.PopupEntity(Loc.GetString("ce-drop-held-items-popup"), target, target, PopupType.SmallCaution);
    }
}
