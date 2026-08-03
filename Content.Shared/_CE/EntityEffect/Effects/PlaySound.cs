using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class PlaySound : CEEntityEffectBase<PlaySound>
{
    public PlaySound()
    {
        EffectTarget = CEEffectTarget.User;
    }

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-play-sound");
}

public sealed partial class CEPlaySoundEffectSystem : CEEntityEffectSystem<PlaySound>
{
    [Dependency] private SharedAudioSystem _audio = default!;

    protected override void Effect(ref CEEntityEffectEvent<PlaySound> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _audio.PlayPredicted(args.Effect.Sound, entity, args.Args.Source,
            args.Effect.Sound.Params.WithVariation(0.15f));
    }
}
