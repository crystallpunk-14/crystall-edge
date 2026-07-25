using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CE.MagicEssence;

public sealed partial class CEMagicEssenceOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    // Base sizes at 100% UI scale and 100% camera zoom; scaled at draw time by both.
    private const float IconSize = 10f;
    private const float IconGap = 1f;
    private const float OutlineOffset = 1f;

    // The essence-count text renders at half the icon's effective scale.
    private const float TextScaleMultiplier = 0.5f;

    // World-space offset above the entity, in meters (scaled by zoom to get screen pixels).
    private const float RowOffset = 0.7f;

    // How long a full fade in/out takes, in seconds.
    private const float FadeDuration = 0.15f;

    // Only the top N essences (by amount) get an icon in the overlay row; the rest collapse into an ellipsis.
    private const int MaxIcons = 5;

    private static readonly Color OutlineColor = Color.Black.WithAlpha(0.85f);
    private static readonly Color TextColor = Color.White;

    private static readonly Vector2 OLeft = new(-OutlineOffset, 0f);
    private static readonly Vector2 ORight = new(OutlineOffset, 0f);
    private static readonly Vector2 OUp = new(0f, -OutlineOffset);
    private static readonly Vector2 ODown = new(0f, OutlineOffset);

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly CEMagicEssenceSystem _essence;
    private readonly SharedInteractionSystem _interaction;

    private readonly Font _font;

    private sealed class FadeEntry
    {
        public float Alpha;
        public List<(ProtoId<CEMagicEssenceTypePrototype> Type, int Amount)> Essences = new();
    }

    // Every entity that is currently hovered OR still fading out from a previous hover.
    private readonly Dictionary<EntityUid, FadeEntry> _fades = new();
    private readonly List<EntityUid> _toRemove = new();

    public CEMagicEssenceOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entityManager.System<SharedTransformSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _essence = _entityManager.System<CEMagicEssenceSystem>();
        _interaction = _entityManager.System<SharedInteractionSystem>();

        var fontResource = _resourceCache.GetResource<FontResource>("/Fonts/_CE/Volkorn/VollkornSC-Bold.ttf");
        _font = new VectorFont(fontResource, 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        var hovered = GetHoveredEssenceTarget(args);

        if (hovered is { } newTarget)
        {
            if (!_fades.TryGetValue(newTarget.Target, out var entry))
            {
                entry = new FadeEntry();
                _fades[newTarget.Target] = entry;
            }

            entry.Essences = newTarget.Essences;
        }

        var frameTime = (float)_timing.FrameTime.TotalSeconds;
        var fadeStep = frameTime / FadeDuration;

        var handle = args.ScreenHandle;
        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var scale = new Vector2(matrix.M11, matrix.M12).Length();
        handle.SetTransform(Matrix3x2.Identity);

        // Camera-zoom factor: 1 at default zoom, >1 zoomed in, <1 zoomed out.
        var zoomFactor = scale / EyeManager.PixelsPerMeter;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var effectiveScale = zoomFactor * uiScale;
        var textScale = effectiveScale * TextScaleMultiplier;

        var iconSize = IconSize * effectiveScale;
        var iconGap = IconGap * effectiveScale;
        var ascent = _font.GetAscent(textScale);

        _toRemove.Clear();

        foreach (var (uid, entry) in _fades)
        {
            var fadeTarget = uid == hovered?.Target ? 1f : 0f;
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

            DrawEntry(handle, matrix, screenPos: Vector2.Transform(_transform.GetWorldPosition(xform), matrix),
                scale, iconSize, iconGap, textScale, ascent, entry);
        }

        foreach (var uid in _toRemove)
        {
            _fades.Remove(uid);
        }
    }

    private void DrawEntry(
        DrawingHandleScreen handle,
        Matrix3x2 matrix,
        Vector2 screenPos,
        float scale,
        float iconSize,
        float iconGap,
        float textScale,
        int ascent,
        FadeEntry entry)
    {
        screenPos.Y -= RowOffset * scale;

        var essences = entry.Essences;
        var shownCount = Math.Min(essences.Count, MaxIcons);
        var hasOverflow = essences.Count > MaxIcons;
        var slotCount = shownCount + (hasOverflow ? 1 : 0);
        var totalWidth = slotCount * iconSize + (slotCount - 1) * iconGap;

        var x = screenPos.X - totalWidth / 2f;
        var alpha = entry.Alpha;
        var iconColor = Color.White.WithAlpha(alpha);
        var textColor = TextColor.WithAlpha(alpha);
        var outlineColor = OutlineColor.WithAlpha(alpha * OutlineColor.A);

        for (var i = 0; i < shownCount; i++)
        {
            var (type, amount) = essences[i];
            if (!_prototypeManager.TryIndex(type, out CEMagicEssenceTypePrototype? essenceProto))
            {
                x += iconSize + iconGap;
                continue;
            }

            var texture = _sprite.Frame0(essenceProto.Icon);
            var iconRect = UIBox2.FromDimensions(new Vector2(x, screenPos.Y), new Vector2(iconSize, iconSize));
            handle.DrawTextureRect(texture, iconRect, iconColor);

            var text = amount.ToString();
            var textDims = handle.GetDimensions(_font, text, textScale);
            // Anchor by baseline (icon bottom - ascent) rather than full line height, since
            // digits have no descenders and the line-height box would otherwise sit too high.
            var textPos = new Vector2(x + iconSize - textDims.X, screenPos.Y + iconSize - ascent);

            handle.DrawString(_font, textPos + OLeft * textScale, text, textScale, outlineColor);
            handle.DrawString(_font, textPos + ORight * textScale, text, textScale, outlineColor);
            handle.DrawString(_font, textPos + OUp * textScale, text, textScale, outlineColor);
            handle.DrawString(_font, textPos + ODown * textScale, text, textScale, outlineColor);
            handle.DrawString(_font, textPos, text, textScale, textColor);

            x += iconSize + iconGap;
        }

        if (hasOverflow)
        {
            const string ellipsis = "...";
            var ellipsisDims = handle.GetDimensions(_font, ellipsis, textScale);
            var ellipsisPos = new Vector2(
                x + (iconSize - ellipsisDims.X) / 2f,
                screenPos.Y + iconSize - ascent);

            handle.DrawString(_font, ellipsisPos + OLeft * textScale, ellipsis, textScale, outlineColor);
            handle.DrawString(_font, ellipsisPos + ORight * textScale, ellipsis, textScale, outlineColor);
            handle.DrawString(_font, ellipsisPos + OUp * textScale, ellipsis, textScale, outlineColor);
            handle.DrawString(_font, ellipsisPos + ODown * textScale, ellipsis, textScale, outlineColor);
            handle.DrawString(_font, ellipsisPos, ellipsis, textScale, textColor);
        }
    }

    private (EntityUid Target, List<(ProtoId<CEMagicEssenceTypePrototype> Type, int Amount)> Essences)? GetHoveredEssenceTarget(in OverlayDrawArgs args)
    {
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mouseMapPos = _eyeManager.PixelToMap(mouseScreenPos);

        if (mouseMapPos.MapId != args.MapId)
            return null;

        if (screen.GetClickedEntity(mouseMapPos) is not { } target)
            return null;

        if (!_entityManager.TryGetComponent<TransformComponent>(target, out var xform) || xform.MapID != args.MapId)
            return null;

        if (_player.LocalEntity is not { } player)
            return null;

        if (!_interaction.InRangeUnobstructed((player, null), (target, xform)))
            return null;

        var essences = _essence.GetEssence(target);
        if (essences.Count == 0)
            return null;

        essences.Sort((a, b) =>
        {
            var byAmount = b.Amount.CompareTo(a.Amount);
            return byAmount != 0 ? byAmount : string.CompareOrdinal(a.Type.Id, b.Type.Id);
        });

        return (target, essences);
    }
}
