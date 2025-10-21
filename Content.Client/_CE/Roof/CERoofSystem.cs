using Content.Shared._CE.Roof;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Client._CE.Roof;

public sealed class CERoofSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private bool _roofVisible = true;
    public bool DisabledByCommand = false;

    private const float TargetAlphaVisible = 1.0f;
    private const float TargetAlphaHidden = 0.0f;
    private const float TransitionRate = 2.0f;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<CERoofComponent> _roofQuery;

    public bool RoofVisibility
    {
        get => _roofVisible && !DisabledByCommand;
        set => _roofVisible = value;
    }

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _roofQuery = GetEntityQuery<CERoofComponent>();

        SubscribeLocalEvent<CERoofComponent, ComponentStartup>(RoofStartup);
        SubscribeLocalEvent<CERoofTogglerComponent, CEToggleRoofVisibilityAction>(OnToggleRoof);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        CheckPlayer();
        AdjustRoofs(frameTime);
    }

    private void CheckPlayer()
    {
        var player = _playerManager.LocalEntity;

        if (_ghostQuery.HasComp(player)) //Dont toggle roofs for ghosts
            return;

        if (!_xformQuery.TryComp(player, out var playerXform))
            return;

        var grid = playerXform.GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var anchored = _map.GetAnchoredEntities(grid.Value, gridComp, playerXform.Coordinates);

        var underRoof = false;
        foreach (var ent in anchored)
        {
            if (!_roofQuery.HasComp(ent))
                continue;

            underRoof = true;
            break;
        }

        _roofVisible = !underRoof;
    }

    private void AdjustRoofs(float frameTime)
    {
        var targetAlpha = (_roofVisible && !DisabledByCommand) ? TargetAlphaVisible : TargetAlphaHidden;
        var change = TransitionRate * frameTime;

        var query = AllEntityQuery<CERoofComponent>();
        while (query.MoveNext(out var uid, out var roof))
        {
            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            var currentAlpha = sprite.Color.A;
            var newAlpha = targetAlpha > currentAlpha
                ? Math.Min(currentAlpha + change, targetAlpha)
                : Math.Max(currentAlpha - change, targetAlpha);

            if (currentAlpha.Equals(newAlpha))
                continue;

            _sprite.SetColor(uid, sprite.Color.WithAlpha(newAlpha));

            if (newAlpha <= 0.01f && sprite.Visible)
            {
                _sprite.SetVisible(uid, false);
            }
            else if (newAlpha > 0.01f && !sprite.Visible)
            {
                _sprite.SetVisible(uid, true);
            }
        }
    }

    private void OnToggleRoof(Entity<CERoofTogglerComponent> ent, ref CEToggleRoofVisibilityAction args)
    {
        if (args.Handled)
            return;

        DisabledByCommand = !DisabledByCommand;
        args.Handled = true;
    }

    private void RoofStartup(Entity<CERoofComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetColor(ent.Owner, sprite.Color.WithAlpha(RoofVisibility ? TargetAlphaVisible : TargetAlphaHidden));
        _sprite.SetVisible(ent.Owner, RoofVisibility);
    }
}

internal sealed class ShowRoof : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "toggle_roof";
    public override string Help => "Toggle roof visibility";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var roofSystem = _entitySystemManager.GetEntitySystem<CERoofSystem>();
        roofSystem.DisabledByCommand = !roofSystem.DisabledByCommand;
    }
}
