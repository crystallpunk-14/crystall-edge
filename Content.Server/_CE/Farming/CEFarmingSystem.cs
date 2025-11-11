using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Farming;
using Content.Shared._CE.Farming.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Farming;

public sealed partial class CEFarmingSystem : CESharedFarmingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly CEDayCycleSystem _dayCycle = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeResources();

        SubscribeLocalEvent<CEPlantComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CEPlantComponent> plant, ref MapInitEvent args)
    {
        var proto = MetaData(plant).EntityPrototype;
        if (proto is null)
            return;

        if (!CanPlant(proto, Transform(plant).Coordinates, null, plant))
        {
            QueueDel(plant);
            return;
        }

        var newTime = _random.NextFloat(plant.Comp.UpdateFrequency);
        plant.Comp.NextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(newTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEPlantComponent>();
        while (query.MoveNext(out var uid, out var plant))
        {
            if (_timing.CurTime <= plant.NextUpdateTime)
                continue;

            plant.NextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(plant.UpdateFrequency);

            var ev = new CEPlantUpdateEvent((uid, plant));
            RaiseLocalEvent(uid, ev);

            var ev2 = new CEAfterPlantUpdateEvent((uid, plant));
            RaiseLocalEvent(uid, ev2);
        }
    }
}
