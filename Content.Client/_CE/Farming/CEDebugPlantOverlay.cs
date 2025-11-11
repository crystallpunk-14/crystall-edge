using System.Numerics;
using Content.Shared._CE.Farming.Components;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Rounding;
using Content.Shared.Weather;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client._CE.Farming;

public sealed class CEDebugPlantOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    private readonly CESharedZLevelsSystem _zLevels = default!;
    private readonly SharedTransformSystem _transform = default!;
    private readonly SharedWeatherSystem _weather = default!;
    private readonly SharedMapSystem _map = default!;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly Font _font;

    public CEDebugPlantOverlay()
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
        var query = _entityManager.EntityQueryEnumerator<CEPlantComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var plant, out var xform))
        {
            if (xform.MapUid != xform.ParentUid)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);
            var screenPos = args.ViewportControl?.WorldToScreen(worldPos) ?? Vector2.Zero;

            if (!_entityManager.TryGetComponent<MapGridComponent>(xform.MapUid, out var gridComp))
                return;

            var depthText = $"Energy: {plant.Energy}/{plant.EnergyMax}\n" +
                            $"Resource: {plant.Resource}/{plant.ResourceMax}\n" +
                            $"Growth Level: {MathF.Round(plant.GrowthLevel * 100)}%\n\n";

            if (_entityManager.TryGetComponent<CEPlantProducingComponent>(uid, out var producing))
            {
                foreach (var (key, entry) in producing.GatherKeys)
                {
                    var readyToHarvest = ContentHelpers.RoundToLevels(entry.Growth, 1, entry.MaxProduce + 1);

                    depthText += $"[PRODUCE: {key}]\n" +
                                 $"- Growth Level: {MathF.Round(entry.Growth * 100)}%\n" +
                                 $"- Ready to harvest: {readyToHarvest}\n";
                }
            }

            args.ScreenHandle.DrawString(_font, screenPos, depthText, Color.White);
        }
    }
}

public sealed class CEShowPlantDebugCommand : LocalizedCommands
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    public override string Command => "showplantdebug";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlayManager.HasOverlay<CEDebugPlantOverlay>())
            _overlayManager.RemoveOverlay<CEDebugPlantOverlay>();
        else
            _overlayManager.AddOverlay(new CEDebugPlantOverlay());
    }
}
