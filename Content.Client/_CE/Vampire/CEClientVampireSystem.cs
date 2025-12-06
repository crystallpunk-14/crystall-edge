using Content.Shared._CE.Vampire;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._CE.Vampire;

public sealed class CEClientVampireSystem : CESharedVampireSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void OnVampireVisualsInit(Entity<CEVampireVisualsComponent> vampire, ref ComponentInit args)
    {
        base.OnVampireVisualsInit(vampire, ref args);

        if (!EntityManager.TryGetComponent(vampire, out SpriteComponent? sprite))
            return;

        if (_sprite.LayerMapTryGet(vampire.Owner, vampire.Comp.FangsMap, out var fangsLayerIndex, false))
            _sprite.LayerSetVisible(vampire.Owner, fangsLayerIndex, true);

        if (_timing.IsFirstTimePredicted)
            SpawnAtPosition(vampire.Comp.EnableVFX, Transform(vampire).Coordinates);
    }

    protected override void OnVampireVisualsShutdown(Entity<CEVampireVisualsComponent> vampire, ref ComponentShutdown args)
    {
        base.OnVampireVisualsShutdown(vampire, ref args);

        if (!EntityManager.TryGetComponent(vampire, out SpriteComponent? sprite))
            return;

        if (_sprite.LayerMapTryGet(vampire.Owner, vampire.Comp.FangsMap, out var fangsLayerIndex, false))
            _sprite.LayerSetVisible(vampire.Owner, fangsLayerIndex, false);

        if (_timing.IsFirstTimePredicted)
            SpawnAtPosition(vampire.Comp.DisableVFX, Transform(vampire).Coordinates);
    }
}
