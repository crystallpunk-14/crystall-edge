using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.Interaction;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CE.HoverBar;

/// <summary>
/// Draws the bars collected by <see cref="CECollectHoverBarsEvent"/> above the entity under the cursor.
/// Bars fade in on hover and out after it; fills move smoothly and leave a white impact trail behind.
/// </summary>
public sealed partial class CEHoverBarOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const string RsiPath = "/Textures/_CE/Interface/HoverBar/bar.rsi";

    // How long a full fade in/out takes, in seconds.
    private const float FadeDuration = 0.15f;

    // How long bars stay after the cursor leaves the entity, in seconds. Hovering again restarts it.
    private const float LingerDuration = 2f;

    // Fill change per second, in bar lengths. The impact layer catches up with the same speed.
    private const float FillSpeed = 2f;

    // How long the impact layer waits before catching up after a drop, in seconds.
    private const float ImpactDelay = 0.3f;

    // World-space offset from the entity origin to the bottom of the lowest bar, in meters.
    private const float HeadOffset = 0.55f;

    // Gap between stacked bars, in sprite pixels.
    private const float BarGap = 1f;

    private static readonly Color ImpactColor = Color.White;

    private readonly SharedTransformSystem _transform;
    private readonly SharedInteractionSystem _interaction;

    private readonly Texture? _background;
    private readonly Texture? _fill;

    private readonly List<CEHoverBar> _collected = new();

    private sealed class LayerAnim
    {
        public float Target;
        public float Shown;
        public float Impact;
        public float Delay;

        public void Reset(float value)
        {
            Target = Shown = Impact = value;
            Delay = 0f;
        }

        public void SetTarget(float value)
        {
            // A new drop restarts the impact delay
            if (value < Target)
                Delay = ImpactDelay;

            Target = value;
        }

        public void Step(float frameTime)
        {
            Shown = MoveTowards(Shown, Target, FillSpeed * frameTime);

            // Healing: the impact layer stays hidden under the fill
            if (Shown >= Impact)
            {
                Impact = Shown;
                return;
            }

            if (Delay > 0f)
            {
                Delay -= frameTime;
                return;
            }

            Impact = MoveTowards(Impact, Shown, FillSpeed * frameTime);
        }
    }

    private sealed class BarAnim
    {
        public string Id = string.Empty;
        public int Order;
        public Color FillColor;
        public Color SecondaryColor;
        public bool HasSecondary;
        public bool Seen;
        public readonly LayerAnim Fill = new();
        public readonly LayerAnim Secondary = new();
    }

    private sealed class FadeEntry
    {
        public float Alpha;
        public float Linger;
        public readonly List<BarAnim> Bars = new();
    }

    // Every entity that is currently hovered OR still fading out from a previous hover.
    private readonly Dictionary<EntityUid, FadeEntry> _fades = new();
    private readonly List<EntityUid> _toRemove = new();

    public CEHoverBarOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entityManager.System<SharedTransformSystem>();
        _interaction = _entityManager.System<SharedInteractionSystem>();

        if (!_resourceCache.TryGetResource<RSIResource>(new ResPath(RsiPath), out var rsi))
            return;

        if (rsi.RSI.TryGetState("background", out var background))
            _background = background.Frame0;

        if (rsi.RSI.TryGetState("fill", out var fill))
            _fill = fill.Frame0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null || _background == null || _fill == null)
            return;

        var hovered = GetHoveredTarget(args);
        if (hovered is { } target)
        {
            if (!_fades.TryGetValue(target, out var hoveredEntry))
            {
                hoveredEntry = new FadeEntry();
                _fades[target] = hoveredEntry;
            }

            hoveredEntry.Linger = LingerDuration;
        }

        var frameTime = (float) _timing.FrameTime.TotalSeconds;
        var fadeStep = frameTime / FadeDuration;

        var handle = args.ScreenHandle;
        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var scale = new Vector2(matrix.M11, matrix.M12).Length();
        handle.SetTransform(Matrix3x2.Identity);

        // Bars are drawn at the same pixel density as world sprites.
        var texel = scale / EyeManager.PixelsPerMeter;

        _toRemove.Clear();

        foreach (var (uid, entry) in _fades)
        {
            if (uid != hovered)
                entry.Linger -= frameTime;

            var fadeTarget = entry.Linger > 0f ? 1f : 0f;
            entry.Alpha = fadeTarget > entry.Alpha
                ? MathF.Min(fadeTarget, entry.Alpha + fadeStep)
                : MathF.Max(fadeTarget, entry.Alpha - fadeStep);

            if (entry.Alpha <= 0f && fadeTarget <= 0f)
            {
                _toRemove.Add(uid);
                continue;
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var xform) || xform.MapID != args.MapId)
            {
                _toRemove.Add(uid);
                continue;
            }

            // Hovered and lingering bars stay live; fading ones keep their last values
            if (fadeTarget > 0f)
                CollectBars(uid, entry);

            foreach (var bar in entry.Bars)
            {
                bar.Fill.Step(frameTime);
                bar.Secondary.Step(frameTime);
            }

            var screenPos = Vector2.Transform(_transform.GetWorldPosition(xform), matrix);
            DrawEntry(handle, screenPos, scale, texel, entry);
        }

        foreach (var uid in _toRemove)
        {
            _fades.Remove(uid);
        }
    }

    private void DrawEntry(DrawingHandleScreen handle, Vector2 screenPos, float scale, float texel, FadeEntry entry)
    {
        var width = _background!.Width * texel;
        var height = _background.Height * texel;
        var gap = BarGap * texel;
        var count = entry.Bars.Count;
        var stackBottom = screenPos.Y - HeadOffset * scale;
        var left = screenPos.X - width / 2f;
        var alpha = entry.Alpha;

        for (var i = 0; i < count; i++)
        {
            var bar = entry.Bars[i];
            var top = stackBottom - (count - i) * height - (count - 1 - i) * gap;
            var rect = UIBox2.FromDimensions(new Vector2(left, top), new Vector2(width, height));

            handle.DrawTextureRect(_background, rect, Color.White.WithAlpha(alpha));

            // Layers are drawn top to bottom, each only where nothing above covers it,
            // so translucent layers never blend with the ones under them while fading.
            var covered = 0f;
            covered = DrawSegment(handle, rect, covered, bar.Fill.Shown, bar.FillColor, alpha);
            covered = DrawSegment(handle, rect, covered, bar.Fill.Impact, ImpactColor, alpha);

            if (!bar.HasSecondary)
                continue;

            covered = DrawSegment(handle, rect, covered, bar.Secondary.Shown, bar.SecondaryColor, alpha);
            DrawSegment(handle, rect, covered, bar.Secondary.Impact, ImpactColor, alpha);
        }
    }

    /// <summary>
    /// Draws the part of a layer between <paramref name="from"/> and <paramref name="to"/>
    /// </summary>
    /// <returns>The new covered length</returns>
    private float DrawSegment(DrawingHandleScreen handle, UIBox2 rect, float from, float to, Color color, float alpha)
    {
        if (to <= from)
            return from;

        var dest = new UIBox2(rect.Left + rect.Width * from, rect.Top, rect.Left + rect.Width * to, rect.Bottom);
        var region = new UIBox2(_fill!.Width * from, 0f, _fill.Width * to, _fill.Height);
        handle.DrawTextureRectRegion(_fill, dest, region, color.WithAlpha(color.A * alpha));
        return to;
    }

    private void CollectBars(EntityUid target, FadeEntry entry)
    {
        if (_player.LocalEntity is not { } viewer)
            return;

        _collected.Clear();
        var ev = new CECollectHoverBarsEvent(viewer, _collected);
        _entityManager.EventBus.RaiseLocalEvent(target, ref ev);


        foreach (var bar in entry.Bars)
        {
            bar.Seen = false;
        }

        foreach (var collected in _collected)
        {
            BarAnim? anim = null;
            foreach (var bar in entry.Bars)
            {
                if (bar.Id != collected.Id)
                    continue;

                anim = bar;
                break;
            }

            var fresh = anim == null;
            if (anim == null)
            {
                anim = new BarAnim { Id = collected.Id };
                entry.Bars.Add(anim);
            }

            anim.Seen = true;
            anim.Order = collected.Order;
            anim.FillColor = collected.FillColor;
            anim.SecondaryColor = collected.SecondaryColor;
            anim.HasSecondary = collected.Secondary != null;

            // A bar that just appeared starts at its value instead of sliding in from zero
            if (fresh)
            {
                anim.Fill.Reset(collected.Fill);
                anim.Secondary.Reset(collected.Secondary ?? 0f);
                continue;
            }

            anim.Fill.SetTarget(collected.Fill);
            anim.Secondary.SetTarget(collected.Secondary ?? 0f);
        }

        entry.Bars.RemoveAll(b => !b.Seen);
        entry.Bars.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    private EntityUid? GetHoveredTarget(in OverlayDrawArgs args)
    {
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        if (_player.LocalEntity is not { } player)
            return null;

        var mouseMapPos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mouseMapPos.MapId != args.MapId)
            return null;

        if (screen.GetClickedEntity(mouseMapPos) is not { } target)
            return null;

        if (!_entityManager.TryGetComponent<TransformComponent>(target, out var xform) || xform.MapID != args.MapId)
            return null;

        if (!_interaction.InRangeUnobstructed((player, null), (target, xform)))
            return null;

        return target;
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }
}
