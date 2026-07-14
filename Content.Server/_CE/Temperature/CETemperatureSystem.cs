using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEntityHeaterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEEntityHeaterComponent> ent, ref PowerChangedEvent args)
    {
        var enabled = args.Powered;
        _ambient.SetAmbience(ent, enabled);
        _pointLight.SetEnabled(ent, enabled);
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
    }
}
