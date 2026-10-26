using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Factions;

/// <summary>
/// Reads each creature's side, including a temporary shift, and stops a party from harming itself.
/// </summary>
public sealed partial class DndAllegianceSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var allegiance = EnsureComp<DndAllegianceComponent>(ev.Mob);
        allegiance.Side = DndSides.Adventurers;
        Dirty(ev.Mob, allegiance);
    }

    [SubscribeLocalEvent]
    private void OnAttackAttempt(EntityUid uid, DndAllegianceComponent component, AttackAttemptEvent args)
    {
        if (args.Disarm || args.Target is not { } target || CanHarm(uid, target))
            return;

        args.Cancel();
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("dnd-attack-party"), uid, uid, PopupType.SmallCaution);
    }

    public ProtoId<DndSidePrototype>? CurrentSide(EntityUid uid)
    {
        ProtoId<DndSidePrototype>? shift = null;
        ProtoId<DndSidePrototype>? home = null;
        if (TryComp<DndAllegianceShiftComponent>(uid, out var shifted))
            shift = shifted.Side;

        if (TryComp<DndAllegianceComponent>(uid, out var allegiance))
            home = allegiance.Side;

        return DndAllegianceRules.Current(home, shift);
    }

    public bool IsParty(EntityUid attacker, EntityUid target)
    {
        return DndAllegianceRules.Between(CurrentSide(attacker), CurrentSide(target)) == DndStance.Party;
    }

    public bool IsOpposition(EntityUid attacker, EntityUid target)
    {
        return DndAllegianceRules.Between(CurrentSide(attacker), CurrentSide(target)) == DndStance.Opposition;
    }

    public bool CanHarm(EntityUid attacker, EntityUid target)
    {
        return DndAllegianceRules.CanHarm(CurrentSide(attacker), CurrentSide(target));
    }
}
