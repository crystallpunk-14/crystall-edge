using Content.Client.Graphics;
using Content.Shared._CE.Murk.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Murk;

public sealed partial class CEMurkDissolvingSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "CEMurkDissolving";

    // Below this Dissolved level the visual effect is fully off; at or above the second value it's at full strength.
    private const float EffectStart = 0.2f;
    private const float EffectEnd = 0.8f;

    private ShaderInstance _shader = default!;
    private CEMurkDissolvingOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(Shader).InstanceUnique();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEMurkDissolvingComponent> ent, ref ComponentStartup args)
    {
        UpdateShader(ent);

        if (ent.Owner == _player.LocalEntity)
            AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEMurkDissolvingComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent))
            SetShader(ent.Owner, false);

        if (ent.Owner == _player.LocalEntity)
            RemoveOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (HasComp<CEMurkDissolvingComponent>(args.Entity))
            AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (_overlay != null)
            return;

        _overlay = new CEMurkDissolvingOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay = null;
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<CEMurkDissolvingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateShader(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeShaderPost(Entity<CEMurkDissolvingComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        _shader.SetParameter("dissolve", GetEffectStrength(ent.Comp.Dissolved));
    }

    private static float GetEffectStrength(float dissolved)
    {
        return Math.Clamp((dissolved - EffectStart) / (EffectEnd - EffectStart), 0f, 1f);
    }

    private void UpdateShader(Entity<CEMurkDissolvingComponent> ent)
    {
        SetShader(ent.Owner, ent.Comp.Enabled && GetEffectStrength(ent.Comp.Dissolved) > 0f);
    }

    private void SetShader(Entity<SpriteComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (enabled)
        {
            _sprite.SetPostShader((entity.Owner, entity.Comp), new SpriteComponent.PostShaderArgs(ContentPostShaderIds.CEMurkDissolving, _shader)
            {
                RaiseShaderEvent = true,
                After = ContentPostShaderIds.AfterBaseEffects,
            });
        }
        else
        {
            _sprite.RemovePostShader((entity.Owner, entity.Comp), ContentPostShaderIds.CEMurkDissolving);
        }
    }
}
