using System.Linq;
using Content.Client.Graphics;
using Content.Shared._CE.Murk.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Murk;

/// <summary>
/// Renders entities affected by <see cref="CEMurkShadowStatusEffectComponent"/> with the murk dissolving
/// shader at full strength. Uses its own post-shader slot, so it never touches the shader driven by real
/// murk dissolution.
/// </summary>
public sealed partial class CEMurkShadowStatusEffectSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "CEMurkDissolving";

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CEMurkShadowStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        SetShader(args.Target, true);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEMurkShadowStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        // Already out of the status effect container here, so this only sees the other active effects.
        if (_statusEffects.EnumerateStatusEffects<CEMurkShadowStatusEffectComponent>(args.Target).Any())
            return;

        SetShader(args.Target, false);
    }

    private void SetShader(Entity<SpriteComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        var hasShader = _sprite.TryGetPostShader(entity.Comp, ContentPostShaderIds.CEMurkShadow, out var existing);

        if (enabled)
        {
            if (hasShader)
                return;

            var shader = ProtoMan.Index(Shader).InstanceUnique();
            shader.SetParameter("dissolve", 1f);
            _sprite.SetPostShader(entity.Comp, new SpriteComponent.PostShaderArgs(ContentPostShaderIds.CEMurkShadow, shader)
            {
                After = ContentPostShaderIds.AfterBaseEffects,
            });
        }
        else if (hasShader)
        {
            _sprite.RemovePostShader(entity.Comp, ContentPostShaderIds.CEMurkShadow);
            existing.Shader.Dispose();
        }
    }
}
