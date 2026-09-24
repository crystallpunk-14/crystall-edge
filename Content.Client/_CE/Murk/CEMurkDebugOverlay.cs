using System.Numerics;
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

    private static readonly Color TooCloseColor = Color.Red;
    private static readonly Color OkColor = Color.White.WithAlpha(0.5f);
    private const float PylonLineWidth = 0.1f;

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

        return _murk.BuildDebugBuffer(args.MapUid, _buffer) || _murk.Pylons.Count > 0;
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

        DrawPylons(args);
    }

    private void DrawPylons(in OverlayDrawArgs args)
    {
        if (_murk.PylonRadius <= 0f)
            return;

        var handle = args.WorldHandle;

        foreach (var pylon in _murk.Pylons)
        {
            if (!_murk.TryProjectPylon(args.MapUid, pylon, out var radius))
                continue;

            var color = pylon.TooClose ? TooCloseColor : OkColor;
            DrawRing(handle, pylon.WorldPos, radius, color);
        }
    }

    // Stacks a few rings since an unfilled DrawCircle is only 1px wide.
    private static void DrawRing(DrawingHandleWorld handle, Vector2 center, float radius, Color color)
    {
        const int steps = 3;
        for (var i = 0; i < steps; i++)
        {
            var r = radius - i * PylonLineWidth;
            if (r <= 0f)
                break;

            handle.DrawCircle(center, r, color, filled: false);
        }
    }
}
