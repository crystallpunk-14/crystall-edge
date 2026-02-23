using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Shared._White.Dash;

public sealed partial class WhiteDashSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteDashComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<WhiteDashComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WhiteDashComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WhiteDashComponent, LandEvent>(OnLand);
    }

    private void OnShutdown(Entity<WhiteDashComponent> ent, ref ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnInit(Entity<WhiteDashComponent> ent, ref ComponentInit args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnLand(Entity<WhiteDashComponent> ent, ref LandEvent args)
    {
        RemCompDeferred<WhiteDashComponent>(ent);
    }

    private void OnMoveAttempt(Entity<WhiteDashComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        //Cant move while dashing
        args.Cancel();
    }

    public void PerformDash(EntityUid ent, EntityCoordinates targetPosition, float speed = 10f, float maxDistance = 3.5f)
    {
        EnsureComp<WhiteDashComponent>(ent, out var dash);
        _audio.PlayPredicted(dash.DashSound, ent, ent);

        var entMapPos = _transform.ToMapCoordinates(Transform(ent).Coordinates);
        var targetMapPos = _transform.ToMapCoordinates(targetPosition);

        var distance = Vector2.Distance(entMapPos.Position, targetMapPos.Position);

        if (distance > maxDistance)
        {
            var direction = (targetMapPos.Position - entMapPos.Position).Normalized();
            var clampedTarget = entMapPos.Position + direction * maxDistance;
            targetMapPos = new MapCoordinates(clampedTarget, entMapPos.MapId);
        }

        var finalTarget = _transform.ToCoordinates(targetMapPos);

        _throwing.TryThrow(ent, finalTarget, speed, null, 0f, 10, true, false, false, false, false);
    }
}
