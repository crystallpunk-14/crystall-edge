using Content.Server._CE.Science.Components;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Effects;

namespace Content.Server._CE.Science.Effects;

public sealed class CEResearchRevealAreaSystem : CEResearchActionEffectSystem<CEResearchRevealArea>
{
    protected override void Effect(ref CEResearchActionEffectEvent<CEResearchRevealArea> args)
    {
        var data = EnsureComp<CEScienceResearchDataComponent>(args.Args.Actor);

        if (!data.Researched.TryGetValue(args.Args.Area, out var researched))
        {
            researched = new HashSet<Vector2i>();
            data.Researched[args.Args.Area] = researched;
        }

        var radius = args.Effect.Radius;
        var center = args.Args.Coordinate;
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
            researched.Add(center + new Vector2i(x, y));
    }
}
