
using Content.Shared._CE.EntityEffect;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Animation.Effects;

public sealed partial class CEEntityEffectSpawnerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEntityEffectSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CEEntityEffectSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextEffectTime = _timing.CurTime + ent.Comp.FirstDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEEntityEffectSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (_timing.CurTime < spawner.NextEffectTime)
                continue;

            spawner.NextEffectTime = _timing.CurTime + spawner.Frequency;

            var pos = Transform(uid).Coordinates;
            var args = new CEEntityEffectArgs(EntityManager, uid, null, Angle.Zero, 1f, uid, pos);

            foreach (var effect in spawner.Effects)
            {
                effect.Effect(args);
            }
        }
    }
}
