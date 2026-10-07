using Content.Shared._CE.MagicTrace.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Analyzers;

namespace Content.Client._CE.MagicTrace;

/// <summary>
/// Draws the networked <see cref="CEMagicTraceComponent.Icon"/> on the trace's sprite.
/// </summary>
public sealed partial class CEClientMagicTraceSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<CEMagicTraceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Icon is not { } icon || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerSetTexture((ent, sprite), CEMagicTraceVisuals.Icon, _sprite.Frame0(icon));
    }
}
