using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.MagicFocus.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.MagicFocus.Systems;

public sealed partial class CEMagicFocusSystem
{
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private void InitCharging()
    {
        SubscribeLocalEvent<CEMagicFocusComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CEMagicFocusComponent, CEMagicFocusChargeDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<CEMagicFocusComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (GetLiquidEssence(target).Count == 0)
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ChargeDelay, new CEMagicFocusChargeDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
        });
    }

    private void OnDoAfter(Entity<CEMagicFocusComponent> ent, ref CEMagicFocusChargeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        var activeTypes = 0;
        foreach (var amount in ent.Comp.CurrentCharge.Values)
        {
            if (amount > 0)
                activeTypes++;
        }

        var totalCharged = 0;
        foreach (var (type, available) in GetLiquidEssence(target))
        {
            if (!_proto.TryIndex(type, out var essenceType) || essenceType.Reagent is not { } reagentId)
                continue;

            var capacity = ent.Comp.Volume.GetValueOrDefault(type, ent.Comp.MinimumVolume);
            var current = ent.Comp.CurrentCharge.GetValueOrDefault(type, 0);
            var room = capacity - current;
            if (room <= 0)
                continue;

            // Introducing a brand new essence type is blocked once the diversity cap is reached -
            // topping up a type that's already present is still fine.
            if (current <= 0 && activeTypes >= ent.Comp.MaxEssenceTypes)
                continue;

            var want = FixedPoint2.New(Math.Min(available, room));
            var removed = FixedPoint2.Zero;
            foreach (var (_, soln) in _solution.EnumerateSolutions(target))
            {
                if (removed >= want)
                    break;

                removed += _solution.RemoveReagent(soln, reagentId, want - removed);
            }

            if (removed <= FixedPoint2.Zero)
                continue;

            if (current <= 0)
                activeTypes++;

            ent.Comp.CurrentCharge[type] = current + removed.Int();
            totalCharged += removed.Int();
        }

        if (totalCharged <= 0)
        {
            _popup.PopupEntity(Loc.GetString("ce-magic-focus-charge-fail"), ent.Owner, args.Args.User);
            return;
        }

        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("ce-magic-focus-charge-success", ("amount", totalCharged)), ent.Owner, args.Args.User);
    }

    /// <summary>
    /// Essence composition of a target's own liquid solutions only (not any embedded/structural essence),
    /// keyed by type, restricted to types that actually have a <see cref="CEMagicEssenceTypePrototype.Reagent"/>
    /// so charging can trace amounts back to a removable reagent.
    /// </summary>
    private Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> GetLiquidEssence(EntityUid target)
    {
        var essences = _essence.GetEssence(target, recursive: false);

        var result = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>();
        foreach (var (type, amount) in essences)
        {
            if (amount <= 0 || !_proto.TryIndex(type, out var essenceType) || essenceType.Reagent is null)
                continue;

            result[type] = amount;
        }

        return result;
    }
}

/// <summary>
/// Raised on a <see cref="CEMagicFocusComponent"/> when it finishes drawing essence out of a clicked
/// liquid container to charge itself.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEMagicFocusChargeDoAfterEvent : SimpleDoAfterEvent;
