using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CE.MagicEssence;

/// <summary>
/// Draws a row of essence icons (with their amount in the bottom-right corner of each icon)
/// above whatever entity is currently under the cursor.
/// </summary>
public sealed class CEMagicEssenceOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const float IconSize = 20f;
    private const float IconGap = 2f;
    private const float RowOffset = 32f;
    private const float OutlineOffset = 1f;

    private static readonly Color OutlineColor = Color.Black.WithAlpha(0.85f);
    private static readonly Color TextColor = Color.White;

    private static readonly Vector2 OLeft = new(-OutlineOffset, 0f);
    private static readonly Vector2 ORight = new(OutlineOffset, 0f);
    private static readonly Vector2 OUp = new(0f, -OutlineOffset);
    private static readonly Vector2 ODown = new(0f, OutlineOffset);

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly CEMagicEssenceSystem _essence;

    private readonly Font _font;

    public CEMagicEssenceOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entityManager.System<SharedTransformSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _essence = _entityManager.System<CEMagicEssenceSystem>();

        var fontResource = _resourceCache.GetResource<FontResource>("/Fonts/_CE/Volkorn/VollkornSC-Bold.ttf");
        _font = new VectorFont(fontResource, 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return;

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mouseMapPos = _eyeManager.PixelToMap(mouseScreenPos);

        if (mouseMapPos.MapId != args.MapId)
            return;

        if (screen.GetClickedEntity(mouseMapPos) is not { } target)
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(target, out var xform) || xform.MapID != args.MapId)
            return;

        var essences = _essence.GetEssence(target);
        if (essences.Count == 0)
            return;

        essences.Sort((a, b) => string.CompareOrdinal(a.Type.Id, b.Type.Id));

        var handle = args.ScreenHandle;
        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var scale = new Vector2(matrix.M11, matrix.M12).Length();
        handle.SetTransform(Matrix3x2.Identity);

        var worldPos = _transform.GetWorldPosition(xform);
        var screenPos = Vector2.Transform(worldPos, matrix);
        screenPos.Y -= RowOffset * scale;

        var iconSize = IconSize * scale;
        var iconGap = IconGap * scale;
        var totalWidth = essences.Count * iconSize + (essences.Count - 1) * iconGap;

        var x = screenPos.X - totalWidth / 2f;

        foreach (var (type, amount) in essences)
        {
            if (!_prototypeManager.TryIndex(type, out CEMagicEssenceTypePrototype? essenceProto))
            {
                x += iconSize + iconGap;
                continue;
            }

            var texture = _sprite.Frame0(essenceProto.Icon);
            var iconRect = UIBox2.FromDimensions(new Vector2(x, screenPos.Y), new Vector2(iconSize, iconSize));
            handle.DrawTextureRect(texture, iconRect, Color.White);

            var text = amount.ToString();
            var textDims = handle.GetDimensions(_font, text, 1f);
            var textPos = new Vector2(x + iconSize - textDims.X, screenPos.Y + iconSize - textDims.Y);

            handle.DrawString(_font, textPos + OLeft, text, 1f, OutlineColor);
            handle.DrawString(_font, textPos + ORight, text, 1f, OutlineColor);
            handle.DrawString(_font, textPos + OUp, text, 1f, OutlineColor);
            handle.DrawString(_font, textPos + ODown, text, 1f, OutlineColor);
            handle.DrawString(_font, textPos, text, 1f, TextColor);

            x += iconSize + iconGap;
        }
    }
}
