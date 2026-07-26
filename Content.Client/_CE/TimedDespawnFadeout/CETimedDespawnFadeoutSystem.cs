using Robust.Client.GameObjects;
using Robust.Shared.Spawners;

namespace Content.Client._CE.TimedDespawnFadeout;

public sealed partial class CETimedDespawnFadeoutSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CETimedDespawnFadeoutComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CETimedDespawnFadeoutComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CETimedDespawnFadeoutComponent> entity, ref ComponentStartup args)
    {
        if (!TryComp<TimedDespawnComponent>(entity, out var despawn))
        {
            Log.Warning($"{ToPrettyString(entity)} has CETimedDespawnFadeout but no TimedDespawn component.");
            return;
        }

        entity.Comp.OriginalLifetime = despawn.Lifetime;
    }

    private void OnShutdown(Entity<CETimedDespawnFadeoutComponent> entity, ref ComponentShutdown args)
    {
        if (MetaData(entity).EntityLifeStage >= EntityLifeStage.Terminating || !TryComp<SpriteComponent>(entity, out var sprite))
            return;

        _sprite.SetColor((entity.Owner, sprite), sprite.Color.WithAlpha(1f));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CETimedDespawnFadeoutComponent, TimedDespawnComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var fadeout, out var despawn, out var sprite))
        {
            if (fadeout.OriginalLifetime <= 0f)
                continue;

            var alpha = Math.Clamp(despawn.Lifetime / fadeout.OriginalLifetime, 0f, 1f);

            if (!sprite.Color.A.Equals(alpha))
                _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
        }
    }
}
