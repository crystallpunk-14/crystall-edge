using Content.Shared._CE.TimedDespawn;
using Robust.Client.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Client._CE.TimedDespawnFadeout;

public sealed partial class CETimedDespawnFadeoutSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CETimedDespawnFadeoutComponent> entity, ref ComponentStartup args)
    {
        if (HasComp<CERandomizedTimedDespawnComponent>(entity))
            return;

        if (!TryComp<TimedDespawnComponent>(entity, out var despawn))
        {
            Log.Warning($"{ToPrettyString(entity)} has CETimedDespawnFadeout but no TimedDespawn component.");
            return;
        }

        entity.Comp.OriginalLifetime = despawn.Lifetime;
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CETimedDespawnFadeoutComponent> entity, ref ComponentShutdown args)
    {
        if (MetaData(entity).EntityLifeStage >= EntityLifeStage.Terminating || !TryComp<SpriteComponent>(entity, out var sprite))
            return;

        _sprite.SetColor((entity.Owner, sprite), sprite.Color.WithAlpha(1f));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CETimedDespawnFadeoutComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var fadeout, out var sprite))
        {
            if (!TryGetElapsed(uid, fadeout, out var elapsed))
                continue;

            var alpha = GetFadeAlpha(elapsed, fadeout.FadeInEnd, fadeout.FadeOutStart);

            if (!sprite.Color.A.Equals(alpha))
                _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
        }
    }

    private bool TryGetElapsed(EntityUid uid, CETimedDespawnFadeoutComponent fadeout, out float elapsed)
    {
        elapsed = 0f;

        if (TryComp<CERandomizedTimedDespawnComponent>(uid, out var randomized))
        {
            if (randomized.Lifetime <= TimeSpan.Zero)
                return false;

            elapsed = Math.Clamp((float)((_timing.CurTime - randomized.SpawnTime) / randomized.Lifetime), 0f, 1f);
            return true;
        }

        if (fadeout.OriginalLifetime <= 0f || !TryComp<TimedDespawnComponent>(uid, out var despawn))
            return false;

        elapsed = Math.Clamp(1f - despawn.Lifetime / fadeout.OriginalLifetime, 0f, 1f);
        return true;
    }

    /// <summary>
    /// Fades in from 0 until <paramref name="fadeInEnd"/>, holds at 1 until <paramref name="fadeOutStart"/>,
    /// then fades back to 0. A threshold at 0 skips its hold/fade-in phase entirely.
    /// </summary>
    public static float GetFadeAlpha(float elapsed, float fadeInEnd, float fadeOutStart)
    {
        if (fadeInEnd > 0f && elapsed <= fadeInEnd)
            return elapsed / fadeInEnd;

        if (elapsed <= fadeOutStart)
            return 1f;

        return fadeOutStart < 1f ? (1f - elapsed) / (1f - fadeOutStart) : 0f;
    }
}
