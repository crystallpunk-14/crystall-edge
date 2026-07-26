using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.ShockWave;

public sealed partial class CEShockWaveOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    private readonly EntProtoId _shaderProto = "CEShockWave";

    /// <summary>
    ///     Maximum number of waves that can be shown on screen at a time.
    /// </summary>
    public const int MaxCount = 10;

    private readonly List<WaveEntry> _activeWaves = new();

    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _falloffPower = new float[MaxCount];
    private readonly float[] _sharpness = new float[MaxCount];
    private readonly float[] _width = new float[MaxCount];
    private readonly float[] _localTime = new float[MaxCount];
    private int _count;

    private struct WaveEntry
    {
        public Vector2 WorldPosition;
        public MapId MapId;
        public float FalloffPower;
        public float Sharpness;
        public float Width;
        public float SpawnTime;
        public float Duration;
    }

    public CEShockWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>(_shaderProto).Instance().Duplicate();
    }

    /// <summary>
    ///     Registers a new shockwave at the given world position. The wave animation
    ///     plays independently from its spawn time and continues even after the source
    ///     entity is removed.
    /// </summary>
    public void AddWave(Vector2 worldPosition, MapId mapId, float falloffPower, float sharpness, float width, float duration)
    {
        _activeWaves.Add(new WaveEntry
        {
            WorldPosition = worldPosition,
            MapId = mapId,
            FalloffPower = falloffPower,
            Sharpness = sharpness,
            Width = width,
            SpawnTime = (float) _timing.RealTime.TotalSeconds,
            Duration = duration,
        });
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        var now = (float) _timing.RealTime.TotalSeconds;

        // Remove entries whose animation has fully elapsed.
        _activeWaves.RemoveAll(w => now - w.SpawnTime >= w.Duration);

        _count = 0;

        foreach (var wave in _activeWaves)
        {
            if (wave.MapId != args.MapId)
                continue;

            var tempCoords = args.Viewport.WorldToLocal(wave.WorldPosition);

            // Normalized coords on the 0–1 plane. Y is flipped because the fragment shader
            // calculates from the bottom while viewport local space goes from the top.
            tempCoords.Y = 1 - (tempCoords.Y / args.Viewport.Size.Y);
            tempCoords.X /= args.Viewport.Size.X;

            _positions[_count] = tempCoords;
            _falloffPower[_count] = wave.FalloffPower;
            _sharpness[_count] = wave.Sharpness;
            _width[_count] = wave.Width;
            _localTime[_count] = now - wave.SpawnTime;
            _count++;

            if (_count == MaxCount)
                break;
        }

        return _count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader?.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);
        _shader?.SetParameter("count", _count);
        _shader?.SetParameter("position", _positions);
        _shader?.SetParameter("falloffPower", _falloffPower);
        _shader?.SetParameter("sharpness", _sharpness);
        _shader?.SetParameter("width", _width);
        _shader?.SetParameter("localTime", _localTime);
        _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
