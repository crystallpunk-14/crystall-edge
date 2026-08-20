using Content.Shared._CE.Speech;
using Content.Shared.GameTicking;

namespace Content.Server._CE.Speech;

public sealed class CEBarkSpawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<CEBarkSpeechComponent>(args.Mob, out var bark))
            return;

        bark.BarkSpeech = args.Profile.BarkVoice;
        bark.BasePitch = args.Profile.BarkPitch;
    }
}
