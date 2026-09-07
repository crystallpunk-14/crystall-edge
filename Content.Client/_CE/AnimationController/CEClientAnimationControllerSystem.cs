using Content.Client._CE.EntityEffect.Effects;
using Content.Shared._CE.AnimationController;
using Robust.Client.GameObjects;
using Robust.Shared.Analyzers;

namespace Content.Client._CE.AnimationController;

/// <summary>Tracks which loop animation is currently playing on this entity.</summary>
[RegisterComponent]
internal sealed partial class CELoopAnimationStateComponent : Component
{
    /// <summary>
    /// Reference to the <see cref="CELoopAnimationData"/> currently active in the animation player.
    /// Null means the loop is stopped (one-shot running, or no animation defined).
    /// </summary>
    public CELoopAnimationData? PlayingAnimation;
}

public sealed partial class CEClientAnimationControllerSystem : EntitySystem
{
    private const string LoopKey = "ce-controller-loop";

    [Dependency] private AnimationPlayerSystem _animPlayer = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void RestartLoop(Entity<CEAnimationControllerComponent> ent, CELoopAnimationStateComponent? state = null, bool ignoreOneShot = false)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!ignoreOneShot && HasComp<CEUserSpriteAnimationComponent>(ent))
            return;

        state ??= EnsureComp<CELoopAnimationStateComponent>(ent);
        var anim = ent.Comp.CurrentAnimation;
        state.PlayingAnimation = anim;

        _animPlayer.Stop(ent.Owner, LoopKey);

        if (anim == null)
            return;

        _animPlayer.Play(ent, CEAnimationTrackBuilders.BuildLoopAnimation(anim), LoopKey);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<CEAnimationControllerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var state = EnsureComp<CELoopAnimationStateComponent>(ent);

        // Skip restart if the same animation is already playing.
        if (state.PlayingAnimation == ent.Comp.CurrentAnimation)
            return;

        RestartLoop(ent, state);
    }

    [SubscribeLocalEvent]
    private void OnLoopCompleted(Entity<CEAnimationControllerComponent> ent, ref AnimationCompletedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (args.Key != LoopKey || !args.Finished)
            return;

        // Clear tracking so RestartLoop always fires even with the same animation data.
        var state = EnsureComp<CELoopAnimationStateComponent>(ent);
        state.PlayingAnimation = null;
        RestartLoop(ent, state);
    }

    [SubscribeLocalEvent]
    private void OnOneShotStarted(Entity<CEUserSpriteAnimationComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<CEAnimationControllerComponent>(ent, out _))
            return;

        var state = EnsureComp<CELoopAnimationStateComponent>(ent);
        state.PlayingAnimation = null;
        _animPlayer.Stop(ent.Owner, LoopKey);
    }

    [SubscribeLocalEvent]
    private void OnOneShotShutdown(Entity<CEUserSpriteAnimationComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp<CEAnimationControllerComponent>(ent, out var controller))
            return;

        // ComponentShutdown fires while the component is still present in HasComp,
        // so pass ignoreOneShot: true to bypass that guard.
        RestartLoop((ent.Owner, controller), ignoreOneShot: true);
    }
}

