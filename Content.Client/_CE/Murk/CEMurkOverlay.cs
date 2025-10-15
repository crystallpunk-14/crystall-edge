using System.Numerics;
using Content.Shared._CE.Murk.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Murk;

public sealed class CEMurkOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly SharedTransformSystem _transform;

    /// <summary>
    ///     Maximum number of observers zones that can be shown on screen at a time.
    ///     If this value is changed, the shader itself also needs to be updated.
    /// </summary>
    public const int MaxCount = 64;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly ShaderInstance _murkShader;

    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _intensities = new float[MaxCount];
    private int _count;

    public CEMurkOverlay()
    {
        IoCManager.InjectDependencies(this);

        _murkShader = _proto.Index<ShaderPrototype>("CEMurk").InstanceUnique();

        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        _count = 0;

        var religionQuery = _entManager.AllEntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (religionQuery.MoveNext(out var uid, out var murk, out var xform))
        {
            if (_count > MaxCount)
                continue;

            if (!murk.Active || xform.MapID != args.MapId)
                continue;

            var mapPos = _transform.GetWorldPosition(uid);

            // To be clear, this needs to use "inside-viewport" pixels.
            // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
            var tempCoords = args.Viewport.WorldToLocal(mapPos);
            tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y; // Local space to fragment space.

            _positions[_count] = tempCoords;
            _intensities[_count] = murk.Intensity;
            _count++;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;


        _murkShader?.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);

        _murkShader?.SetParameter("count", _count);
        _murkShader?.SetParameter("position", _positions);
        _murkShader?.SetParameter("radius", _intensities);

        _murkShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_murkShader);
        worldHandle.DrawRect(args.WorldAABB, Color.White);
        worldHandle.UseShader(null);
    }
}

