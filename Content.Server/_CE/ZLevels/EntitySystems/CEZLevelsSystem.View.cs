using Content.Server._CE.ZLevels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private void InitView()
    {
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<CEZLevelViewerComponent, EntParentChangedMessage>(OnViewerParentChange);
        SubscribeLocalEvent<CEZLevelViewerComponent, MoveEvent>(OnViewerMove);
    }

    private void OnViewerMove(Entity<CEZLevelViewerComponent> ent, ref MoveEvent args)
    {
        foreach (var eye in ent.Comp.Eyes)
        {
            _transform.SetWorldPosition(eye, _transform.GetWorldPosition(ent));
        }
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        var viewer = EnsureComp<CEZLevelViewerComponent>(ev.Entity);
        UpdateViewer((ev.Entity, viewer));
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        RemComp<CEZLevelViewerComponent>(ev.Entity);
    }

    private void OnViewerParentChange(Entity<CEZLevelViewerComponent> ent, ref EntParentChangedMessage args)
    {
        UpdateViewer(ent);
    }

    private void UpdateViewer(Entity<CEZLevelViewerComponent> ent)
    {
        var eyes = ent.Comp.Eyes;
        foreach (var eye in ent.Comp.Eyes)
        {
            QueueDel(eye);
        }
        eyes.Clear();

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var xform = Transform(ent);
        var map = xform.MapUid;
        if (map is null)
            return;

        var globalPos = _transform.GetWorldPosition(xform);

        for (var i = 1; i <= MaxZLevelsBelowRendering; i++)
        {
            if (!TryMapOffset(map.Value, -i, out _, out var mapUidBelow))
                break;

            var newEye = SpawnAtPosition(null, new EntityCoordinates(mapUidBelow.Value, globalPos));

            Transform(newEye).GridTraversal = false;
            _viewSubscriber.AddViewSubscriber(newEye, actor.PlayerSession);
            eyes.Add(newEye);
        }

        for (var i = 1; i <= MaxZLevelsAboveRendering; i++)
        {
            if (!TryMapOffset(map.Value, i, out _, out var mapUidAbove))
                break;

            var newEye = SpawnAtPosition(null, new EntityCoordinates(mapUidAbove.Value, globalPos));

            Transform(newEye).GridTraversal = false;
            _viewSubscriber.AddViewSubscriber(newEye, actor.PlayerSession);
            eyes.Add(newEye);
        }
    }
}
