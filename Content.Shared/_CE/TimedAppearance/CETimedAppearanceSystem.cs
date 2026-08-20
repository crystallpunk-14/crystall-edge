using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared._CE.TimedAppearance;

/// <summary>
/// Manages timed visual state overrides that use <see cref="AppearanceComponent"/> as the
/// networking layer. Predicted: runs on both client and server.
/// After <paramref name="duration"/> seconds the previous appearance key is automatically restored.
/// </summary>
public sealed partial class CETimedAppearanceSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<CETimedAppearanceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;

            _appearance.SetData(uid, CEAnimationAppearanceVisuals.Key, "default");
            RemComp<CETimedAppearanceComponent>(uid);
        }
    }

    /// <summary>
    /// Activates a named visual state on <paramref name="entity"/> for <paramref name="duration"/> seconds
    /// Only one timed appearance may be active at a time; calling again replaces the current override
    /// and restores the appearance to the state it had before the previous call.
    /// </summary>
    /// <param name="entity">The entity whose appearance to override.</param>
    /// <param name="key">Named visual state to activate</param>
    /// <param name="duration">How long the override lasts, in seconds.</param>
    [PublicAPI]
    public void SetTimedAppearance(EntityUid entity, string key, float duration)
    {
        _appearance.SetData(entity, CEAnimationAppearanceVisuals.Key, key);

        var comp = EnsureComp<CETimedAppearanceComponent>(entity);
        comp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(duration);
        Dirty(entity, comp);
    }

    /// <summary>
    /// Immediately cancels any active timed appearance override and restores the previous state.
    /// </summary>
    [PublicAPI]
    public void CancelTimedAppearance(EntityUid entity)
    {
        _appearance.SetData(entity, CEAnimationAppearanceVisuals.Key, "default");
        RemComp<CETimedAppearanceComponent>(entity);
    }
}
