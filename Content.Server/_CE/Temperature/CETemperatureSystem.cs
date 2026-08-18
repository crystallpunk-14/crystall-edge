using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._CE.Temperature;

public sealed partial class CETemperatureSystem : EntitySystem
{
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEntityHeaterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CESolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEEntityHeaterComponent> ent, ref PowerChangedEvent args)
    {
        SetPowerVisuals(ent, args.Powered);
    }

    private void OnPowerChanged(Entity<CESolutionHeaterComponent> ent, ref PowerChangedEvent args)
    {
        SetPowerVisuals(ent, args.Powered);
    }

    private void SetPowerVisuals(EntityUid uid, bool enabled)
    {
        _ambient.SetAmbience(uid, enabled);
        _pointLight.SetEnabled(uid, enabled);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEEntityHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var heater, out var itemPlacer))
        {
            if (_timing.CurTime < heater.NextHeat)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            heater.NextHeat = _timing.CurTime + heater.Frequency;

            foreach (var placed in itemPlacer.PlacedEntities)
            {
                _temperature.ChangeHeat(placed, heater.Power);
            }
        }

        var solutionQuery = EntityQueryEnumerator<CESolutionHeaterComponent, ItemPlacerComponent>();
        while (solutionQuery.MoveNext(out var uid, out var heater, out var itemPlacer))
        {
            if (_timing.CurTime < heater.NextHeat)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            heater.NextHeat = _timing.CurTime + heater.Frequency;

            foreach (var placed in itemPlacer.PlacedEntities)
            {
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(placed))
                {
                    if (!soln.Comp.Solution.CanReact)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, heater.Power);
                }
            }
        }
    }
}
