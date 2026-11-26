using Content.Server.Fluids.EntitySystems;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Audio;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CESmokeCloudEffectSystem : CEEntityEffectSystem<SmokeCloud>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    protected override void Effect(ref CEEntityEffectEvent<SmokeCloud> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!_solutionContainer.TryGetSolution(entity, args.Effect.Solution, out _, out var solution) || solution.Volume <= 0)
            return;

        var coordinates = Transform(entity).Coordinates;
        var smoke = SpawnAtPosition(args.Effect.SmokeProto, coordinates);

        _smoke.StartSmoke(smoke, solution, args.Effect.Duration, (int) solution.Volume);
        _audio.PlayPvs(args.Effect.Sound, coordinates, AudioHelpers.WithVariation(0.125f));
    }
}
