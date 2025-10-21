using Content.Server._CE.ZLevels.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEStationZLevelsSystem
{
    [Dependency] private readonly LinkedEntitySystem _linkedEntity = default!;

    private void InitializePortals()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CEZLevelAutoPortalComponent, MapInitEvent>(OnPortalMapInit);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var query = EntityQueryEnumerator<CEZLevelAutoPortalComponent>();
        while (query.MoveNext(out var uid, out var portal))
        {
            InitPortal((uid, portal));
        }
    }

    private void OnPortalMapInit(Entity<CEZLevelAutoPortalComponent> autoPortal, ref MapInitEvent args)
    {
        InitPortal(autoPortal);
    }

    private void InitPortal(Entity<CEZLevelAutoPortalComponent> autoPortal)
    {
        var mapId = Transform(autoPortal).MapUid;
        if (mapId is null)
            return;

        if (!TryMapOffset(mapId.Value, autoPortal.Comp.ZLevelOffset, out var offsetMap))
            return;

        var currentWorldPos = _transform.GetWorldPosition(autoPortal);
        var targetMapPos = new MapCoordinates(currentWorldPos, offsetMap.Value);

        var otherSidePortal = Spawn(autoPortal.Comp.OtherSideProto, targetMapPos);

        _transform.SetWorldRotation(otherSidePortal, _transform.GetWorldRotation(autoPortal));
        if (_linkedEntity.TryLink(autoPortal, otherSidePortal, true))
            RemComp<CEZLevelAutoPortalComponent>(autoPortal);
    }
}
