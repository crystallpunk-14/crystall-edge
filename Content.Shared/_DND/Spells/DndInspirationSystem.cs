using Content.Shared._DND.Abilities;
using Content.Shared._DND.Features;
using Content.Shared.Charges.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Spells;

/// <summary>
/// Gives a creature one Bardic Inspiration die, and spends it on a failed d20 test when the die could change the result.
/// </summary>
public sealed partial class DndInspirationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public const int DurationMinutes = 60;
    public const int RangeFeet = 60;

    [SubscribeLocalEvent]
    private void OnInspire(EntityUid uid, DndBardicInspirationSourceComponent component, DndInspireEvent args)
    {
        if (!_net.IsServer)
            return;

        if (args.Target == uid || !HasComp<MobStateComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-inspiration-target"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (component.Uses <= 0)
        {
            _popup.PopupEntity(Loc.GetString("dnd-inspiration-spent"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var die = EnsureComp<DndBardicInspirationComponent>(args.Target);
        die.Sides = component.DieSides;
        die.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(DurationMinutes);
        Dirty(args.Target, die);

        component.Uses--;
        Dirty(uid, component);
        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("dnd-inspiration-give", ("die", component.DieSides), ("uses", component.Uses)), uid, uid, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("dnd-inspiration-receive", ("die", component.DieSides)), args.Target, args.Target, PopupType.Medium);
    }

    public void RecoverLongRest(EntityUid uid, DndBardicInspirationSourceComponent source)
    {
        source.Uses = source.MaxUses;
        Dirty(uid, source);
        if (source.Action is { } action)
        {
            _charges.SetMaxCharges(action, source.MaxUses);
            _charges.SetCharges(action, source.Uses);
        }
    }

    /// <summary>
    /// Adds the inspiration die to a failed d20 test when the highest face could meet the difficulty.
    /// A natural 1 on an attack is passed in already resolved, so callers skip this for an automatic miss.
    /// </summary>
    public int Improve(EntityUid uid, int naturalRoll, int bonus, int difficultyClass)
    {
        var total = naturalRoll + bonus;
        if (total >= difficultyClass || !_net.IsServer)
            return total;

        if (!TryComp<DndBardicInspirationComponent>(uid, out var die))
            return total;

        if (_timing.CurTime >= die.ExpiresAt)
        {
            RemComp<DndBardicInspirationComponent>(uid);
            return total;
        }

        if (total + die.Sides < difficultyClass)
            return total;

        var rolled = _random.Next(1, die.Sides + 1);
        RemComp<DndBardicInspirationComponent>(uid);
        total += rolled;
        _popup.PopupEntity(Loc.GetString("dnd-inspiration-roll", ("die", rolled), ("total", total)), uid, uid, PopupType.Medium);
        return total;
    }
}
