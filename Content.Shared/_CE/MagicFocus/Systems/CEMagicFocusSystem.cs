using Content.Shared._CE.MagicEssence.Events;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicFocus.Components;
using Content.Shared._CE.MagicFocus.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicFocus.Systems;

/// <summary>
/// Resolves the pool of thaumaturgical essence a player can draw from via their active magic
/// foci (worn clothing and held items), and lets other systems check/spend against it.
/// </summary>
public sealed partial class CEMagicFocusSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;

    [Dependency] private EntityQuery<CEMagicFocusComponent> _focusQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicFocusClothingComponent, InventoryRelayedEvent<CEGetMagicFocusEssenceEvent>>(OnClothingGetEssence);
        SubscribeLocalEvent<CEMagicFocusInhandComponent, CEGetMagicFocusEssenceEvent>(OnInhandGetEssence);
        SubscribeLocalEvent<CEMagicFocusComponent, CEMagicEssenceCalculationEvent>(OnFocusEssenceCalculation);
    }

    private void OnClothingGetEssence(Entity<CEMagicFocusClothingComponent> ent, ref InventoryRelayedEvent<CEGetMagicFocusEssenceEvent> args)
    {
        if (!_inventory.InSlotWithAnyFlags(ent.Owner, ent.Comp.Slots))
            return;

        args.Args.Sources.Add(ent.Owner);
    }

    private void OnInhandGetEssence(Entity<CEMagicFocusInhandComponent> ent, ref CEGetMagicFocusEssenceEvent args)
    {
        args.Sources.Add(ent.Owner);
    }

    // Lets essence scanners (and anything else reading essence composition) see what's stored in the focus.
    private void OnFocusEssenceCalculation(Entity<CEMagicFocusComponent> ent, ref CEMagicEssenceCalculationEvent args)
    {
        if (args.Handled)
            return;

        foreach (var (type, amount) in ent.Comp.CurrentEssence)
        {
            args.Essences[type] = args.Essences.GetValueOrDefault(type) + amount;
        }
    }

    /// <summary>
    /// Returns true if the sum of the given essence types across all of the user's active foci
    /// meets the required amounts. Does not spend anything.
    /// </summary>
    public bool HasEnoughEssence(EntityUid user, Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> cost)
    {
        var sources = GetSources(user);

        foreach (var (type, required) in cost)
        {
            if (GetAvailable(sources, type) < required)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks and, if sufficient, deducts the given essence cost from the user's active foci.
    /// Deducts greedily across foci in collection order (equipped foci, then held items).
    /// Returns false and deducts nothing if the user doesn't have enough of any required type.
    /// </summary>
    public bool TrySpendEssence(EntityUid user, Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> cost)
    {
        var sources = GetSources(user);

        foreach (var (type, required) in cost)
        {
            if (GetAvailable(sources, type) < required)
                return false;
        }

        foreach (var (type, required) in cost)
        {
            var remaining = required;
            foreach (var source in sources)
            {
                if (remaining <= 0)
                    break;

                var current = GetCurrent(source, type);
                if (current <= 0)
                    continue;

                var take = Math.Min(current, remaining);
                SetCurrent(source, type, current - take);
                remaining -= take;
            }
        }

        return true;
    }

    private List<Entity<CEMagicFocusComponent>> GetSources(EntityUid user)
    {
        var ev = new CEGetMagicFocusEssenceEvent();
        RaiseLocalEvent(user, ev);

        foreach (var held in _hands.EnumerateHeld(user))
        {
            RaiseLocalEvent(held, ev);
        }

        var result = new List<Entity<CEMagicFocusComponent>>();
        foreach (var uid in ev.Sources)
        {
            if (_focusQuery.TryComp(uid, out var focus))
                result.Add((uid, focus));
        }

        return result;
    }

    private static int GetAvailable(List<Entity<CEMagicFocusComponent>> sources, ProtoId<CEMagicEssenceTypePrototype> type)
    {
        var total = 0;
        foreach (var source in sources)
        {
            total += GetCurrent(source, type);
        }

        return total;
    }

    private static int GetCurrent(Entity<CEMagicFocusComponent> focus, ProtoId<CEMagicEssenceTypePrototype> type)
    {
        return focus.Comp.CurrentEssence.GetValueOrDefault(type, 0);
    }

    private void SetCurrent(Entity<CEMagicFocusComponent> focus, ProtoId<CEMagicEssenceTypePrototype> type, int value)
    {
        focus.Comp.CurrentEssence[type] = value;
        Dirty(focus);
    }
}
