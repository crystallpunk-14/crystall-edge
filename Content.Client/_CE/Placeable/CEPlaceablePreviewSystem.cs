using System.Linq;
using Content.Client.Gameplay;
using Content.Shared._CE.Placeable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Placeable;

/// <summary>
/// Draws a ghost of the entity the held <see cref="CEPlaceableComponent"/> item would place under the cursor,
/// green when the tile is valid and red when it isn't. The placement-rotate key turns it.
/// </summary>
public sealed partial class CEPlaceablePreviewSystem : EntitySystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private CEPlaceableSystem _placeable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<CEPlaceableComponent> _placeableQuery = default!;

    private static readonly Color ValidColor = new(48, 255, 48, 128);
    private static readonly Color InvalidColor = new(255, 48, 48, 128);

    // Client-only visuals, never saved.
    private EntityUid? _ghost;
    private EntProtoId? _ghostPrototype;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EditorRotateObject, InputCmdHandler.FromDelegate(_ => Rotate(), handle: false))
            .Register<CEPlaceablePreviewSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<CEPlaceablePreviewSystem>();
        ClearGhost();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!TryGetHeldPlaceable(out var user, out var item))
        {
            ClearGhost();
            return;
        }

        if (!TryGetMouseCoordinates(out var mouse)
            || _state.CurrentState is not GameplayStateBase gameplay
            || !_placeable.IsPlacementClick(user, gameplay.GetClickedEntity(mouse))
            || !_interaction.InRangeUnobstructed(user, _transform.ToCoordinates(mouse))
            || !_placeable.TryGetTarget(item, user, _transform.ToCoordinates(mouse), out var target))
        {
            HideGhost();
            return;
        }

        var ghost = EnsureGhost(item.Comp.Prototype);
        var grid = target.Value.Grid;
        var tileCenter = _transform.ToMapCoordinates(_map.GridTileToLocal(grid, grid.Comp, target.Value.Tile));

        // Placing below targets another map, but the ghost stays on the user's one, over the open tile.
        _transform.SetMapCoordinates(ghost, new MapCoordinates(tileCenter.Position, mouse.MapId));
        _transform.SetWorldRotation(ghost, _transform.GetWorldRotation(grid) + target.Value.Direction.ToAngle());

        _sprite.SetVisible(ghost.AsNullable(), true);
        _sprite.SetColor(ghost.AsNullable(), _placeable.CanPlace(item, target.Value) ? ValidColor : InvalidColor);
    }

    private void Rotate()
    {
        if (!TryGetHeldPlaceable(out _, out var item) || !item.Comp.Rotatable)
            return;

        var direction = item.Comp.Direction;
        RaisePredictiveEvent(new CEPlaceableRotateEvent(GetNetEntity(item), direction.GetClockwise90Degrees()));
    }

    private bool TryGetHeldPlaceable(out EntityUid user, out Entity<CEPlaceableComponent> item)
    {
        user = default;
        item = default;

        if (_player.LocalEntity is not { } player
            || _hands.GetActiveItem(player) is not { } held
            || !_placeableQuery.TryComp(held, out var placeable))
            return false;

        user = player;
        item = (held, placeable);
        return true;
    }

    private bool TryGetMouseCoordinates(out MapCoordinates coordinates)
    {
        coordinates = MapCoordinates.Nullspace;

        if (_ui.CurrentlyHovered is not IViewportControl viewport)
            return false;

        coordinates = viewport.PixelToMap(_input.MouseScreenPosition.Position);
        return coordinates.MapId != MapId.Nullspace;
    }

    private Entity<SpriteComponent> EnsureGhost(EntProtoId prototype)
    {
        if (_ghost is { } existing && _ghostPrototype == prototype && TryComp<SpriteComponent>(existing, out var existingSprite))
            return (existing, existingSprite);

        ClearGhost();

        var ghost = Spawn(null, MapCoordinates.Nullspace);
        var sprite = EnsureComp<SpriteComponent>(ghost);

        // The prototype is spawned only to copy its sprite: its other components (fixtures, ladders) must not act.
        var dummy = Spawn(prototype, MapCoordinates.Nullspace);
        if (TryComp<SpriteComponent>(dummy, out var dummySprite))
            _sprite.CopySprite((dummy, dummySprite), (ghost, sprite));
        Del(dummy);

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            sprite.LayerSetShader(i, "unshaded");
        }

        _ghost = ghost;
        _ghostPrototype = prototype;
        return (ghost, sprite);
    }

    private void HideGhost()
    {
        if (_ghost is { } ghost && TryComp<SpriteComponent>(ghost, out var sprite))
            _sprite.SetVisible((ghost, sprite), false);
    }

    private void ClearGhost()
    {
        if (_ghost is { } ghost)
            Del(ghost);

        _ghost = null;
        _ghostPrototype = null;
    }
}
