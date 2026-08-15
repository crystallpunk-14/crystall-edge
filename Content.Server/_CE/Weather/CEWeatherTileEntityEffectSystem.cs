using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Weather;

namespace Content.Server._CE.Weather;

/// <summary>
/// Applies <see cref="CEWeatherTileEntityEffectComponent"/>'s effects at a struck tile's coordinates,
/// as reported by <see cref="CEWeatherTileEffectsSystem"/>. No target entity is set, so effects like
/// <c>SpawnEntity</c> resolve against the coordinates directly.
/// </summary>
public sealed partial class CEWeatherTileEntityEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEWeatherTileEntityEffectComponent, CEWeatherTileAffectedEvent>(OnTileAffected);
    }

    private void OnTileAffected(Entity<CEWeatherTileEntityEffectComponent> ent, ref CEWeatherTileAffectedEvent args)
    {
        var effectArgs = new CEEntityEffectArgs(EntityManager, ent.Owner, null, Angle.Zero, 1f, null,
            args.Coordinates, ent.Comp.Power);

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }
    }
}
