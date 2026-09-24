using System.Numerics;
using Content.Shared._CE.Murk.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Murk;

/// <summary>
/// Draws murk as a translucent layer in <see cref="OverlaySpace.WorldSpaceBelowFOV"/> - above
/// entities but below lighting and FOV. It never reads the screen texture: every z-level pass
/// alpha-blends its own layer, so stacked levels darken towards opaque instead of multiplying
/// each other into pure black.
/// </summary>
public sealed partial class CEMurkOverlay : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _config = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly ShaderInstance? _murkShader;
    private readonly EntityQuery<CEMurkedMapComponent> _mapQuery;
    private readonly CEClientMurkSystem _murk;

    private readonly ProtoId<ShaderPrototype> _shader = "CEMurk";

    public CEMurkOverlay()
    {
        IoCManager.InjectDependencies(this);

        _murkShader = _proto.Index(_shader).InstanceUnique();
        _mapQuery = _entManager.GetEntityQuery<CEMurkedMapComponent>();
        _murk = _entManager.System<CEClientMurkSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        if (!_mapQuery.TryGetComponent(args.MapUid, out var murkedMap))
            return false;

        return _murk.BuildRenderBuffer((args.MapUid, murkedMap));
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_mapQuery.TryGetComponent(args.MapUid, out var murkedMap))
            return;

        var bounds = args.WorldBounds;
        var color = murkedMap.MurkColor;

        _murkShader?.SetParameter("MURK_COLOR", new Vector3(color.R, color.G, color.B));
        _murkShader?.SetParameter("corner_bl", bounds.BottomLeft);
        _murkShader?.SetParameter("corner_br", bounds.BottomRight);
        _murkShader?.SetParameter("corner_tl", bounds.TopLeft);
        _murkShader?.SetParameter("corner_tr", bounds.TopRight);
        _murkShader?.SetParameter("baseIntensity", murkedMap.LerpedIntensity);
        _murkShader?.SetParameter("count", murkedMap.Count);
        _murkShader?.SetParameter("positions", murkedMap.Positions);
        _murkShader?.SetParameter("radii", murkedMap.Radii);
        _murkShader?.SetParameter("strengths", murkedMap.Strengths);
        _murkShader?.SetParameter("isBoundary", murkedMap.IsBoundary);
        _murkShader?.SetParameter("maxOpacity", _config.GetCVar(CCVars.CEMurkMaxOpacity));
        _murkShader?.SetParameter("noiseStrength", _config.GetCVar(CCVars.CEMurkNoiseStrength));

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_murkShader);
        worldHandle.DrawRect(bounds, Color.White);
        worldHandle.UseShader(null);
    }
}
