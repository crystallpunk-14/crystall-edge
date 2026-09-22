using System.Numerics;
using Content.Shared._CE.Shiny;
using Content.Shared.Hands;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CE.Shiny;

public sealed partial class CEShinySystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEShinyComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CEShinyComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextShinyTime = _timing.CurTime + NextDelay(ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CEShinyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Skip entities that are not lying directly on the map or a grid (e.g. in inventory/containers).
            if (xform.ParentUid != xform.GridUid && xform.ParentUid != xform.MapUid)
                continue;

            if (now < comp.NextShinyTime)
                continue;

            comp.NextShinyTime = now + NextDelay(comp);
            SpawnSparkle((uid, comp));
        }
    }

    private void SpawnSparkle(Entity<CEShinyComponent> ent)
    {
        var angle = _random.NextFloat() * MathF.PI * 2f;
        var dist = _random.NextFloat() * ent.Comp.Radius;
        var offset = new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);
        SpawnAtPosition(ent.Comp.Effect, Transform(ent).Coordinates.Offset(offset));
    }

    private TimeSpan NextDelay(CEShinyComponent comp)
    {
        var min = comp.MinFrequency.TotalSeconds;
        var max = comp.MaxFrequency.TotalSeconds;
        return TimeSpan.FromSeconds(min + _random.NextDouble() * (max - min));
    }
}
