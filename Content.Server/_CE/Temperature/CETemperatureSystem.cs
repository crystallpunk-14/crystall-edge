using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Timing;

namespace Content.Server._CE.Temperature;

public sealed partial class CETemperatureSystem : EntitySystem
{
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    /// <summary>
    /// Reusable buffer of the solutions heated by a single heater this tick.
    /// </summary>
    private readonly List<Entity<SolutionComponent>> _heatedSolutions = new();

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEEntityHeaterComponent> ent, ref PowerChangedEvent args)
    {
        SetPowerVisuals(ent, args.Powered);
    }

    [SubscribeLocalEvent]
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

            if (itemPlacer.PlacedEntities.Count == 0)
                continue;

            // Power is shared between all placed entities, like in EntityHeaterSystem
            var energy = heater.Power / itemPlacer.PlacedEntities.Count;
            foreach (var placed in itemPlacer.PlacedEntities)
            {
                _temperature.ChangeHeat(placed, energy);
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

            _heatedSolutions.Clear();
            foreach (var placed in itemPlacer.PlacedEntities)
            {
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(placed))
                {
                    if (soln.Comp.Solution.CanReact)
                        _heatedSolutions.Add(soln);
                }
            }

            if (_heatedSolutions.Count == 0)
                continue;

            // Power is shared between all heated solutions
            var energy = heater.Power / _heatedSolutions.Count;
            foreach (var soln in _heatedSolutions)
            {
                _solutionContainer.AddThermalEnergy(soln, energy);
            }
        }
    }
}
