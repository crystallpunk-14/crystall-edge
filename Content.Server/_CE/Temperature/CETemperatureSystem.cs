using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Placeable;
using Robust.Server.GameObjects;

namespace Content.Server._CE.Temperature;

/// <inheritdoc/>
public sealed class CETemperatureSystem : EntitySystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEntityHeaterComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEEntityHeaterComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower >= args.DrawRate;
        _ambient.SetAmbience(ent,  enabled);
        _pointLight.SetEnabled(ent, enabled);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEEntityHeaterComponent, PowerConsumerComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var heater, out var power, out var itemPlacer))
        {
            if (power.ReceivedPower < power.DrawRate)
                continue;

            foreach (var placed in itemPlacer.PlacedEntities)
            {
                _temperature.ChangeHeat(placed, heater.Power);
            }
        }
    }
}
