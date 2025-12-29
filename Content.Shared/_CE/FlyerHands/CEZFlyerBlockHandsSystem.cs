using Content.Shared._CE.ZLevels.Flight;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;

namespace Content.Shared._CE.FlyerHands;

public sealed class CEZFlyerBlockHandsSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZFlyerBlockHandsComponent, CEFlightStartedEvent>(OnStartFlight);
        SubscribeLocalEvent<CEZFlyerBlockHandsComponent, DidEquipHandEvent>(EquipEvent);
        SubscribeLocalEvent<CEZFlyerBlockHandsComponent, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(Entity<CEZFlyerBlockHandsComponent> ent, ref PickupAttemptEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyer))
            return;

        if (flyer.Active)
            args.Cancel();
    }

    private void EquipEvent(Entity<CEZFlyerBlockHandsComponent> ent, ref DidEquipHandEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyer))
            return;

        if (flyer.Active)
        {
            DropAll(ent);
        }
    }

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
