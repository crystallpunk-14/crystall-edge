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
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly SharedTransformSystem _transform;

    /// <summary>
    ///     Maximum number of observers zones that can be shown on screen at a time.
    ///     If this value is changed, the shader itself also needs to be updated.
    /// </summary>
    public const int MaxCount = 64;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly ShaderInstance? _murkShader;

    private float _baseIntensity = 0;
    private Vector2 _playerPos = Vector2.Zero;
    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _intensities = new float[MaxCount];
    private int _count;

    private readonly HashSet<EntityUid> _seen = [];
    private readonly Dictionary<EntityUid, MurkEntry> _murkBuffer = new();

    private const float LerpStep = 0.01f;
    private sealed class MurkEntry
    {
        public Vector2 Position;
        public float Intensity;
    }

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

        _playerPos = args.Viewport.Eye.Position.Position;

        float targetMapIntensity = 0;
        if (_entManager.TryGetComponent<CEMurkedMapComponent>(args.MapUid, out var murkedMap))
            targetMapIntensity = murkedMap.Intensity;

        _baseIntensity = MathHelper.Lerp(_baseIntensity, targetMapIntensity, LerpStep);

        _seen.Clear();
        var query = _entManager.AllEntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var murk, out var xform))
        {
            if (!murk.Active || xform.MapID != args.MapId)
                continue;

            _seen.Add(uid);

            var mapPos = _transform.GetWorldPosition(uid);

            if (!_murkBuffer.TryGetValue(uid, out var entry))
            {
                entry = new MurkEntry
                {
                    Intensity = 0,
                    Position = mapPos,
                };
                _murkBuffer[uid] = entry;
            }

            entry.Position = Vector2.Lerp(entry.Position, mapPos, LerpStep);
            entry.Intensity = MathHelper.Lerp(entry.Intensity, murk.Active ? murk.Intensity : 0, LerpStep);
        }

        var toRemove = new List<EntityUid>();
        foreach (var (uid, entry) in _murkBuffer)
        {
            if (!_seen.Contains(uid))
                entry.Intensity = MathHelper.Lerp(entry.Intensity, 0, LerpStep);

            if (Math.Abs(entry.Intensity) < 0.01f)
                toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            _murkBuffer.Remove(uid);
        }

        _count = 0;
        foreach (var entry in _murkBuffer.Values)
        {
            if (_count >= MaxCount)
                break;

            var tempCoords = args.Viewport.WorldToLocal(entry.Position);
            tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;

            _positions[_count] = tempCoords;
            _intensities[_count] = entry.Intensity;
            _count++;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;


        _murkShader?.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);

        _murkShader?.SetParameter("baseIntensity", _baseIntensity);
        _murkShader?.SetParameter("playerPos", _playerPos);
        _murkShader?.SetParameter("count", _count);
        _murkShader?.SetParameter("position", _positions);
        _murkShader?.SetParameter("intensities", _intensities);

        _murkShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_murkShader);
        worldHandle.DrawRect(args.WorldAABB, Color.White);
        worldHandle.UseShader(null);
    }
}

