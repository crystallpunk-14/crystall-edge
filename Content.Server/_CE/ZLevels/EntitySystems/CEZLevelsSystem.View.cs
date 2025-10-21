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

        SubscribeLocalEvent<CEZLevelEyeComponent, ComponentStartup>(ZLevelEyeStartup);
        SubscribeLocalEvent<CEZLevelEyeComponent, ComponentShutdown>(ZLevelEyeShutdown);

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

    private void ZLevelEyeStartup(Entity<CEZLevelEyeComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Target is null)
            return;

        _viewSubscriber.AddViewSubscriber(ent, ent.Comp.Target);
    }

    private void ZLevelEyeShutdown(Entity<CEZLevelEyeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Target is null)
            return;

        _viewSubscriber.RemoveViewSubscriber(ent, ent.Comp.Target);
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

        var mapsBelow = GetAllMapsBelow(map.Value);
        var globalPos = _transform.GetWorldPosition(xform);

        foreach (var mapBelow in mapsBelow)
        {
            var newEye = SpawnAtPosition(null, new EntityCoordinates(mapBelow, globalPos));

            Transform(newEye).GridTraversal = false;
            AddComp(newEye,
                new CEZLevelEyeComponent
                {
                    Target = actor.PlayerSession,
                }
            );

            eyes.Add(newEye);
        }
    }
}
