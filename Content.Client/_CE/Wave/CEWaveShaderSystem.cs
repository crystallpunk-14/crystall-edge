using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._CE.Wave;

public sealed class CEWaveShaderSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ShaderInstance _shader = default!;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index<ShaderPrototype>("CEWave").InstanceUnique();
        _enabled = _cfg.GetCVar(CCVars.CEWaveShaderEnabled);

        SubscribeLocalEvent<CEWaveShaderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEWaveShaderComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CEWaveShaderComponent, BeforePostShaderRenderEvent>(OnBeforeShaderPost);

        Subs.CVar(_cfg, CCVars.CEWaveShaderEnabled, GlobalChangeWaveShader, true);
    }

    private void GlobalChangeWaveShader(bool enable)
    {
        var query = EntityQueryEnumerator<CEWaveShaderComponent>();
        while (query.MoveNext(out var uid, out var wave))
        {
            if (enable)
            {
                wave.Offset = _random.NextFloat(0, 1000);
                SetShader(uid, _shader);
            }
            else
            {
                SetShader(uid, null);
            }
        }
    }

    private void OnStartup(Entity<CEWaveShaderComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.Offset = _random.NextFloat(0, 1000);
        SetShader(entity.Owner, _shader);
    }

    private void OnShutdown(Entity<CEWaveShaderComponent> entity, ref ComponentShutdown args)
    {
        SetShader(entity.Owner, null);
    }

    private void SetShader(Entity<SpriteComponent?> entity, ShaderInstance? instance)
    {
        if (!Resolve(entity, ref entity.Comp, false) || !_enabled)
            return;

        entity.Comp.PostShader = instance;
        entity.Comp.GetScreenTexture = instance is not null;
        entity.Comp.RaiseShaderEvent = instance is not null;
    }

    private void OnBeforeShaderPost(Entity<CEWaveShaderComponent> entity, ref BeforePostShaderRenderEvent args)
    {
        if (!_enabled)
            return;

        _shader.SetParameter("Speed", entity.Comp.Speed);
        _shader.SetParameter("Dis", entity.Comp.Dis);
        _shader.SetParameter("Offset", entity.Comp.Offset);
    }
}
