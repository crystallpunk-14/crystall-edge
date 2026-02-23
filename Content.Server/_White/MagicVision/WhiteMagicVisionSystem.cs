using Content.Shared._White.MagicVision;
using Content.Shared._White.MagicVision.Components;
using Robust.Shared.Timing;
using Content.Shared.StatusEffectNew;

namespace Content.Server._White.MagicVision;

public sealed class WhiteMagicVisionSystem : WhiteSharedMagicVisionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteMagicVisionStatusEffectComponent, StatusEffectRelayedEvent<GetVisMaskEvent>>(OnGetVisMask);

        SubscribeLocalEvent<WhiteMagicVisionStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<WhiteMagicVisionStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnGetVisMask(Entity<WhiteMagicVisionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetVisMaskEvent> args)
    {
        var appliedMask = (int)WhiteMagicVisionStatusEffectComponent.VisibilityMask;
        var newArgs = args.Args;

        newArgs.VisibilityMask |= appliedMask;
        args = args with { Args = newArgs };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WhiteMagicVisionMarkerComponent>();
        while (query.MoveNext(out var uid, out var marker))
        {
            if (marker.EndTime == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < marker.EndTime)
                continue;

            QueueDel(uid);
        }
    }

    private void OnApplied(Entity<WhiteMagicVisionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _eye.RefreshVisibilityMask(args.Target);
    }

    private void OnRemoved(Entity<WhiteMagicVisionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _eye.RefreshVisibilityMask(args.Target);
    }
}
