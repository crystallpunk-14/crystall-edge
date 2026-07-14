using Content.Shared._CE.Animation.Core;

namespace Content.Shared._CE.Animation.SpawnAnimation;

public sealed partial class CESpawnAnimationSystem : EntitySystem
{
    [Dependency] private CESharedAnimationActionSystem _animation = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CESpawnAnimationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CESpawnAnimationComponent> ent, ref MapInitEvent args)
    {
        _animation.TryPlayAnimationToAngle(ent, ent.Comp.Animation, forceCancel: true);
    }
}
