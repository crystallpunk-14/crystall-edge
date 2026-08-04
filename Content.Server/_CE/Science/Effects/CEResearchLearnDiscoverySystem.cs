using Content.Shared._CE.Knowledge;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Science.Effects;

public sealed partial class CEResearchLearnDiscoverySystem : CEResearchActionEffectSystem<CEResearchLearnDiscovery>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEScienceSystem _science = default!;
    [Dependency] private CEKnowledgeSystem _knowledge = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier DiscoverySound =
        new SoundPathSpecifier(new ResPath("/Audio/_CE/Effects/knowledge_learned.ogg"));

    protected override void Effect(ref CEResearchActionEffectEvent<CEResearchLearnDiscovery> args)
    {
        if (!_science.TryGetSingleton(out var science)
            || !science.Areas.TryGetValue(args.Args.Area, out var areaCells)
            || !areaCells.TryGetValue(args.Args.Coordinate, out var cell)
            || cell is not CEScienceDiscoveryCell discoveryCell)
        {
            return;
        }

        if (!_proto.TryIndex(discoveryCell.Discovery, out var discovery))
            return;

        // Already known, or couldn't afford this discovery's own cost - the action's generic Cost
        // is 0 for this effect, so the real gating happens here.
        if (_knowledge.Knows(args.Args.Actor, discovery.Knowledge))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Args.Actor);

        if (!_science.TrySpendPoints((args.Args.Actor, data), discovery.Cost))
            return;

        _science.TryLearnDiscovery(args.Args.Actor, discoveryCell.Discovery);

        _audio.PlayPvs(DiscoverySound, args.Args.Table);
    }
}
