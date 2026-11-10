using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.PulseAmulet;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._CE.PulseAmulet;

public sealed partial class CEPulseProjectorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityQuery<CEMurkDissolvingComponent> _dissolvingQuery = default!;

    [SubscribeLocalEvent]
    private void OnBoundUIOpened(Entity<CEPulseProjectorComponent> ent, ref BoundUIOpenedEvent args)
    {
        var markers = CollectMarkers();
        _ui.SetUiState(ent.Owner, CEPulseProjectorUiKey.Key, new CEPulseProjectorBoundUserInterfaceState(markers));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        List<CEPulseMarker>? markers = null;
        var alert = false;

        var query = EntityQueryEnumerator<CEPulseProjectorComponent>();
        while (query.MoveNext(out var uid, out var projector))
        {
            if (curTime < projector.NextUpdate)
                continue;

            projector.NextUpdate = curTime + projector.UpdateInterval;

            // All projectors see the same amulets, so the scan is shared between them within one tick.
            if (markers == null)
            {
                markers = CollectMarkers();
                alert = markers.Exists(m => m.Status is CEPulseMarkerStatus.Critical or CEPulseMarkerStatus.Dead);
            }

            _appearance.SetData(uid, CEPulseProjectorVisuals.Alert, alert);

            if (_ui.IsUiOpen(uid, CEPulseProjectorUiKey.Key))
                _ui.SetUiState(uid, CEPulseProjectorUiKey.Key, new CEPulseProjectorBoundUserInterfaceState(markers));
        }
    }

    private List<CEPulseMarker> CollectMarkers()
    {
        var markers = new List<CEPulseMarker>();

        var query = EntityQueryEnumerator<CEPulseAmuletComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var amulet, out var battery))
        {
            // An empty amulet transmits nothing.
            if (_battery.GetCharge((uid, battery)) <= 0f)
                continue;

            var mapCoords = _transform.GetMapCoordinates(uid);
            if (mapCoords.MapId == MapId.Nullspace)
                continue;

            // Map-relative coordinates, so the client can place the marker even if the wearer is outside its PVS.
            var coords = _transform.ToCoordinates(mapCoords);
            markers.Add(new CEPulseMarker(GetNetCoordinates(coords), GetStatus(amulet.Wearer)));
        }

        return markers;
    }

    private CEPulseMarkerStatus GetStatus(EntityUid? wearer)
    {
        if (wearer is not { } body || !Exists(body))
            return CEPulseMarkerStatus.Removed;

        // A body dissolved by the murk is a shadow husk now, it still counts as dead.
        if (_dissolvingQuery.TryComp(body, out var dissolving) && dissolving.Converted)
            return CEPulseMarkerStatus.Dead;

        if (_mobState.IsDead(body))
            return CEPulseMarkerStatus.Dead;

        if (_mobState.IsCritical(body))
            return CEPulseMarkerStatus.Critical;

        return CEPulseMarkerStatus.Worn;
    }
}
