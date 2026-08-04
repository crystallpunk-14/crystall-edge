using System.Numerics;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Effects;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._CE.Science.Effects;

public sealed partial class CEResearchCheckHypothesisSystem : CEResearchActionEffectSystem<CEResearchCheckHypothesis>
{
    [Dependency] private CEScienceSystem _science = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier HypothesisSound = new SoundCollectionSpecifier("PaperScribbles");

    protected override void Effect(ref CEResearchActionEffectEvent<CEResearchCheckHypothesis> args)
    {
        var coordinate = args.Args.Coordinate;
        float? bestDistance = null;

        if (_science.TryGetSingleton(out var science) && science.Areas.TryGetValue(args.Args.Area, out var areaCells))
        {
            var data = EnsureComp<CEScienceResearchDataComponent>(args.Args.Actor);
            var researched = data.Researched.TryGetValue(args.Args.Area, out var set) ? set : new HashSet<Vector2i>();

            foreach (var (candidate, cell) in areaCells)
            {
                if (cell is not (CEScienceStarCell or CEScienceOfferedStarCell) || researched.Contains(candidate))
                    continue;

                var delta = candidate - coordinate;
                var distance = new Vector2(delta.X, delta.Y).Length();

                if (distance > args.Effect.MaxRadius)
                    continue;

                if (bestDistance is null || distance < bestDistance)
                    bestDistance = distance;
            }
        }

        var message = new CEResearchTableHypothesisResultMessage(args.Args.Area, coordinate, bestDistance, args.Effect.Duration);
        _userInterface.ServerSendUiMessage(args.Args.Table, CEResearchTableUiKey.Key, message, args.Args.Actor);

        _audio.PlayPvs(HypothesisSound, args.Args.Table);
    }
}
