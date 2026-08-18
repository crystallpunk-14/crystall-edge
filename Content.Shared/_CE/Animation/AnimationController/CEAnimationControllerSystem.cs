using Content.Shared._CE.TimedAppearance;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared._CE.AnimationController;

public sealed partial class CEAnimationControllerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // When a timed appearance expires, restore the controller's fallback key.
        SubscribeLocalEvent<CETimedAppearanceComponent, ComponentShutdown>(OnTimedAppearanceShutdown);
    }

    private void OnTimedAppearanceShutdown(Entity<CETimedAppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<CEAnimationControllerComponent>(ent, out var controller))
            return;

        // Bypass the HasComp guard — we're inside shutdown so the component is technically still present.
        _appearance.SetData(ent, CEAnimationAppearanceVisuals.Key, controller.CurrentAppearanceKey ?? "default");
    }

    /// <summary>
    /// Re-evaluates the fallback animation and appearance for <paramref name="uid"/> by raising
    /// <see cref="CECalculateCurrentAnimationEvent"/> and <see cref="CECalculateCurrentAppearanceEvent"/>,
    /// then stores the winners into <see cref="CEAnimationControllerComponent"/> and calls
    /// <see cref="OnVisualsChanged"/> for concrete subclasses to react.
    /// </summary>
    [PublicAPI]
    public void RefreshVisuals(Entity<CEAnimationControllerComponent?> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var animEv = new CECalculateCurrentAnimationEvent();
        RaiseLocalEvent(ent, animEv);

        var appearEv = new CECalculateCurrentAppearanceEvent();
        RaiseLocalEvent(ent, appearEv);

        var animChanged = ent.Comp.CurrentAnimation != animEv.Animation;
        var appearChanged = ent.Comp.CurrentAppearanceKey != appearEv.AppearanceKey;

        if (!animChanged && !appearChanged)
            return;

        ent.Comp.CurrentAnimation = animEv.Animation;
        ent.Comp.CurrentAppearanceKey = appearEv.AppearanceKey;
        Dirty(ent);

        // Don't overwrite an active timed appearance — let it expire naturally.
        if (!HasComp<CETimedAppearanceComponent>(ent))
            _appearance.SetData(ent, CEAnimationAppearanceVisuals.Key, appearEv.AppearanceKey ?? "default");
    }
}

/// <summary>
/// Raised on an entity (via <see cref="CEAnimationControllerSystem.RefreshVisuals"/>) to determine
/// which <em>fallback</em> appearance key should currently be active.
/// </summary>
public sealed partial class CECalculateCurrentAppearanceEvent : EntityEventArgs
{
    public string? AppearanceKey { get; private set; }
    public int Priority { get; private set; } = int.MinValue;

    public void Set(string key, int priority)
    {
        if (priority <= Priority)
            return;
        AppearanceKey = key;
        Priority = priority;
    }
}

/// <summary>
/// Raised on an entity (via <see cref="CEAnimationControllerSystem.RefreshVisuals"/>) to determine
/// which <em>fallback</em> looping <see cref="CELoopAnimationData"/> should currently play on the entity.
/// </summary>
public sealed partial class CECalculateCurrentAnimationEvent : EntityEventArgs
{
    public CELoopAnimationData? Animation { get; private set; }
    public int Priority { get; private set; } = int.MinValue;

    public void Set(CELoopAnimationData animation, int priority)
    {
        if (priority <= Priority)
            return;

        Animation = animation;
        Priority = priority;
    }
}
