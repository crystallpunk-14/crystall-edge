using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Strips every item from the target's inventory and hands and scatters them in random directions.
/// </summary>
public sealed partial class Undress : CEEntityEffectBase<Undress>
{
    [DataField]
    public float ThrowPower = 5f;

    [DataField]
    public float Distance = 1.5f;
}

public sealed partial class CEUndressEffectSystem : CEEntityEffectSystem<Undress>
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<Undress> args)
    {
        if (!_net.IsServer)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        var items = new List<EntityUid>();

        if (_inventory.TryGetContainerSlotEnumerator(target, out var enumerator))
        {
            while (enumerator.MoveNext(out var container))
            {
                if (container.ContainedEntity is { } item
                    && _inventory.TryUnequip(target, container.ID, force: true))
                {
                    items.Add(item);
                }
            }
        }

        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (_hands.TryDrop(target, held, checkActionBlocker: false))
                items.Add(held);
        }

        foreach (var item in items)
        {
            var dir = _random.NextAngle().ToVec() * args.Effect.Distance;
            _throwing.TryThrow(item, dir, args.Effect.ThrowPower);
        }
    }
}
