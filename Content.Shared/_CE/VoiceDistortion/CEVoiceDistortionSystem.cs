using Content.Shared._CE.Speech;
using Content.Shared.Chat;
using Content.Shared.Inventory;

namespace Content.Shared._CE.VoiceDistortion;

public sealed partial class CEVoiceDistortionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnTransformSpeakerName(Entity<CEVoiceDistortionClothingComponent> ent, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        args.Args.VoiceName = Loc.GetString(ent.Comp.VoiceName);
    }

    [SubscribeLocalEvent]
    private void OnBarkSpeechOverride(Entity<CEVoiceDistortionClothingComponent> ent, ref InventoryRelayedEvent<CEBarkSpeechOverrideEvent> args)
    {
        args.Args.BarkSpeech = ent.Comp.BarkSpeech;
        args.Args.BasePitch = ent.Comp.BarkPitch;
    }
}
