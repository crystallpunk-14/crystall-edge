using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Murk;

public sealed partial class CEMurkDebugOverlay : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _config = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly ShaderInstance? _shader;
    private readonly CEClientMurkSystem _murk;

    private readonly ProtoId<ShaderPrototype> _shaderProto = "CEMurkDebug";

    private readonly CEMurkDebugBuffer _buffer = new();

    public CEMurkDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _proto.Index(_shaderProto).InstanceUnique();
        _murk = _entManager.System<CEClientMurkSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        return _murk.BuildDebugBuffer(args.MapUid, _buffer);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var bounds = args.WorldBounds;

        _shader?.SetParameter("corner_bl", bounds.BottomLeft);
        _shader?.SetParameter("corner_br", bounds.BottomRight);
        _shader?.SetParameter("corner_tl", bounds.TopLeft);
        _shader?.SetParameter("corner_tr", bounds.TopRight);
        _shader?.SetParameter("freeCount", _buffer.FreeCount);
        _shader?.SetParameter("freePositions", _buffer.FreePositions);
        _shader?.SetParameter("freeRadii", _buffer.FreeRadii);
        _shader?.SetParameter("freeStrengths", _buffer.FreeStrengths);
        _shader?.SetParameter("wallCount", _buffer.WallCount);
        _shader?.SetParameter("wallPositions", _buffer.WallPositions);
        _shader?.SetParameter("wallRadii", _buffer.WallRadii);
        _shader?.SetParameter("wallStrengths", _buffer.WallStrengths);
        _shader?.SetParameter("threshold", _config.GetCVar(CCVars.CEMurkThreshold));

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(bounds, Color.White);
        worldHandle.UseShader(null);
    }
}
