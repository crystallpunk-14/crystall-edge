using Content.Shared._CE.ZLevels.Flight;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.FlyerHands;

public sealed partial class CEZFlyerBlockHandsSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnPickupAttempt(Entity<CEZFlyerBlockHandsComponent> ent, ref PickupAttemptEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyer))
            return;

        if (flyer.Active)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void EquipEvent(Entity<CEZFlyerBlockHandsComponent> ent, ref DidEquipHandEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyer))
            return;

        if (flyer.Active)
        {
            DropAll(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnStartFlight(Entity<CEZFlyerBlockHandsComponent> ent, ref CEFlightStartedEvent args)
    {
        DropAll(ent.Owner);
    }

    private void DropAll(EntityUid uid)
    {
        foreach (var handId in _hands.EnumerateHands(uid))
        {
            _hands.DoDrop(uid, handId);
        }
    }
}
