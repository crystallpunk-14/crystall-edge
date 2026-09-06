using System.Numerics;
using Content.Shared._CE.ZLevels.Core.Components;
using Robust.Client.GameObjects;

namespace Content.Client._CE.ZLevels.Core;

/// <summary>
/// Client-only ownership marker for the sprite baseline captured for the current
/// Z-physics and sprite component instances.
/// </summary>
[RegisterComponent]
internal sealed partial class CEZVisualBaselineStateComponent : Component
{
    public CEZPhysicsComponent? ZPhysics;
    public SpriteComponent? Sprite;
    public Vector2 SpriteOffsetBaseline;
    public int DrawDepthBaseline;
}

/// <summary>
/// Neutral client event raised by the single owner of the exclusive sprite
/// shutdown subscription, before visual consumers release their state.
/// </summary>
[ByRefEvent]
internal readonly record struct CESpriteVisualReleasingEvent(SpriteComponent Component);

/// <summary>
/// Owns the engine-exclusive sprite lifecycle subscription and exposes a
/// component-directed seam for independent visual consumers.
/// </summary>
internal sealed partial class CESpriteVisualLifecycleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpriteComponent, ComponentShutdown>(OnSpriteShutdown);
    }

    private void OnSpriteShutdown(Entity<SpriteComponent> ent, ref ComponentShutdown args)
    {
        var releasing = new CESpriteVisualReleasingEvent(ent.Comp);
        RaiseLocalEvent(ent.Owner, ref releasing);
    }
}
