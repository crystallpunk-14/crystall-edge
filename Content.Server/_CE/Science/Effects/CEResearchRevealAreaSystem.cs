using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Effects;

namespace Content.Server._CE.Science.Effects;

public sealed class CEResearchRevealAreaSystem : CEResearchActionEffectSystem<CEResearchRevealArea>
{
    [Dependency] private CEScienceSystem _science = default!;

    protected override void Effect(ref CEResearchActionEffectEvent<CEResearchRevealArea> args)
    {
        var data = EnsureComp<CEScienceResearchDataComponent>(args.Args.Actor);
        _science.RevealArea((args.Args.Actor, data), args.Args.Area, args.Args.Coordinate, args.Effect.Radius);
    }
}
