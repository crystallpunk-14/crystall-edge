using System.Numerics;
using Content.Shared._CE.Weather.Clouds;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.Weather.Clouds;

/// <summary>
/// Draws drifting, morphing cloud shadows over maps with a <see cref="CECloudsOverlayComponent"/>.
/// Runs in <see cref="OverlaySpace.BeforeLighting"/>, after the map's ambient light color has
/// cleared <c>LightRenderTarget</c> but before point lights are additively drawn into it - so it
/// only darkens the ambient baseline and never affects point lights.
/// </summary>
public sealed partial class CECloudsOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    private readonly ShaderInstance _shader;
    private readonly ProtoId<ShaderPrototype> _shaderProto = "CECloudShadow";

    public CECloudsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(_shaderProto).Instance().Duplicate();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        return _entity.HasComponent<CECloudsOverlayComponent>(args.MapUid);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent<CECloudsOverlayComponent>(args.MapUid, out var clouds))
            return;

        var time = (float) _timing.CurTime.TotalSeconds;
        var windOffset = clouds.WindVelocity * time;
        var seedOffset = new Vector2(clouds.Seed * 12.9898f, clouds.Seed * 78.233f);
        var shadowColor = new Vector3(clouds.ShadowColor.R, clouds.ShadowColor.G, clouds.ShadowColor.B);
        var bounds = args.WorldBounds;

        _shader.SetParameter("corner_bl", bounds.BottomLeft);
        _shader.SetParameter("corner_br", bounds.BottomRight);
        _shader.SetParameter("corner_tl", bounds.TopLeft);
        _shader.SetParameter("corner_tr", bounds.TopRight);
        _shader.SetParameter("wind_offset", windOffset);
        _shader.SetParameter("seed_offset", seedOffset);
        _shader.SetParameter("time_z", clouds.EvolutionSpeed * time);
        _shader.SetParameter("frequency", clouds.Frequency);
        _shader.SetParameter("octaves", clouds.Octaves);
        _shader.SetParameter("lacunarity", clouds.Lacunarity);
        _shader.SetParameter("gain", clouds.Gain);
        _shader.SetParameter("coverage", clouds.Coverage);
        _shader.SetParameter("shadow_strength", clouds.ShadowStrength);
        _shader.SetParameter("shadow_color", shadowColor);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(bounds, Color.White);
        worldHandle.UseShader(null);
    }
}
