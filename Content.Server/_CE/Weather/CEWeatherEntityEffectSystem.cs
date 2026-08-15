using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Weather;

namespace Content.Server._CE.Weather;

/// <summary>
/// Applies <see cref="CEWeatherEntityEffectComponent"/>'s effects to entities exposed to weather,
/// as reported by <see cref="CEWeatherEffectsSystem"/>.
/// </summary>
public sealed partial class CEWeatherEntityEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEWeatherEntityEffectComponent, CEWeatherEntityAffectedEvent>(OnEntityAffected);
    }

    private void OnEntityAffected(Entity<CEWeatherEntityEffectComponent> ent, ref CEWeatherEntityAffectedEvent args)
    {
        var effectArgs = new CEEntityEffectArgs(EntityManager, args.Target, null, Angle.Zero, 1f, args.Target,
            Transform(args.Target).Coordinates, ent.Comp.Power);

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }
    }
}
