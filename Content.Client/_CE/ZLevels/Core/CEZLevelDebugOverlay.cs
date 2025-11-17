using System.Numerics;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Weather;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client._CE.ZLevels.Core;

public sealed class CEZLevelDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    private readonly CESharedZLevelsSystem _zLevels = default!;
    private readonly SharedTransformSystem _transform = default!;
    private readonly SharedWeatherSystem _weather = default!;
    private readonly SharedMapSystem _map = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly Font _font;

    public CEZLevelDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _zLevels = _entityManager.System<CESharedZLevelsSystem>();
        _weather = _entityManager.System<SharedWeatherSystem>();
        _map = _entityManager.System<SharedMapSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();

        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entityManager.EntityQueryEnumerator<CEZPhysicsComponent, CEActiveZPhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out _, out var xform))
        {
            if (xform.MapUid != xform.ParentUid)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);
            var screenPos = args.ViewportControl?.WorldToScreen(worldPos) ?? Vector2.Zero;

            var localPos = MathF.Round(zPhys.LocalPosition, 2);
            var groundDis = MathF.Round(_zLevels.DistanceToGround(uid, out var sticky), 2);
            var velocity = MathF.Round(zPhys.Velocity, 2);

            if (!_entityManager.TryGetComponent<MapGridComponent>(xform.MapUid, out var gridComp))
                return;
            var weatherAffect = _weather.CanWeatherAffect(xform.MapUid.Value, gridComp, _map.GetTileRef(xform.MapUid.Value, gridComp, xform.Coordinates));

            var depthText = $"ZLocalHeight: {localPos}\n" +
                            $"Distance to ground: {groundDis}\n" +
                            $"Velocity: {velocity}\n" +
                            $"Sticky: {sticky}\n" +
                            $"Weather affect: {weatherAffect}";

            args.ScreenHandle.DrawString(_font, screenPos, depthText, Color.White);
        }
    }
}

public sealed class CEShowZLevelDebugCommand : LocalizedCommands
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    public override string Command => "showzleveldebug";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlayManager.HasOverlay<CEZLevelDebugOverlay>())
            _overlayManager.RemoveOverlay<CEZLevelDebugOverlay>();
        else
            _overlayManager.AddOverlay(new CEZLevelDebugOverlay());
    }
}
