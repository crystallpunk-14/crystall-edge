using Content.Shared._CE.Animation.Core;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.Animation.SpawnAnimation;

public sealed partial class CESpawnAnimationSystem : EntitySystem
{
    [Dependency] private CESharedAnimationActionSystem _animation = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CESpawnAnimationComponent> ent, ref MapInitEvent args)
    {
        // Start keyframes fire at once: otherwise clients may render the entity untouched for one tick before the animation arrives.
        _animation.TryPlayAnimationToAngle(ent, ent.Comp.Animation, forceCancel: true, fireStartKeyframes: true);
    }
}
