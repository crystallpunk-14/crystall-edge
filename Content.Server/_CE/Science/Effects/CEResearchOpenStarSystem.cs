using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._CE.Science.Effects;

public sealed partial class CEResearchOpenStarSystem : CEResearchActionEffectSystem<CEResearchOpenStar>
{
    [Dependency] private CEScienceSystem _science = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier OpenSound = new SoundCollectionSpecifier("PaperScribbles");

    protected override void Effect(ref CEResearchActionEffectEvent<CEResearchOpenStar> args)
    {
        if (!_science.TryGetSingleton(out var science)
            || !science.Areas.TryGetValue(args.Args.Area, out var areaCells)
            || !areaCells.TryGetValue(args.Args.Coordinate, out var cell)
            || cell is not CEScienceStarCell star)
        {
            // Not a star, or already opened/resolved by someone else - no re-rolling.
            return;
        }

        var candidates = _science.GetNextDiscovery(science, args.Args.Area, star.Rarity);
        if (candidates.Count == 0)
        {
            Log.Warning($"CEResearchOpenStarSystem: no discoveries left to offer for area {args.Args.Area}, rarity {star.Rarity}.");
            return;
        }

        areaCells[args.Args.Coordinate] = new CEScienceOfferedStarCell(star.Rarity, candidates);

        _audio.PlayPvs(OpenSound, args.Args.Table);
    }
}
