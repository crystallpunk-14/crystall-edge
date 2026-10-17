using Content.Server._CE.ZLevels.Chat;
using Content.Server.Chat.Systems;
using Content.Shared._CE.Radio;
using Content.Shared._CE.Radio.Components;
using Content.Shared._CE.Radio.Prototypes;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Radio;

/// <summary>
/// Relays speech heard by radio booths to every powered loudspeaker on a matching frequency in range.
/// </summary>
public sealed partial class CERadioSystem : CESharedRadioSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private CEZLevelsSpeakingSystem _zSpeaking = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    // Transient per-tick dedup: several booths hearing the same phrase make each speaker say it once.
    private readonly HashSet<(EntityUid Speaker, EntityUid Source, string Message)> _recentlySent = new();
    private readonly List<EntityUid> _targets = new();

    // Transient per-broadcast: speakers currently relaying, and sessions that already got the phrase in their chat log.
    private readonly HashSet<EntityUid> _broadcastingSpeakers = new();
    private readonly HashSet<ICommonSession> _broadcastHeard = new();
    private readonly List<ICommonSession> _alreadyHeard = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    [SubscribeLocalEvent]
    private void OnListen(Entity<CERadioMicrophoneComponent> ent, ref ListenEvent args)
    {
        if (!ent.Comp.Enabled || ent.Comp.Frequencies.Count == 0)
            return;

        if (HasComp<CERadioSpeakerComponent>(args.Source) || HasComp<CEZLevelSpeechTransmitterComponent>(args.Source))
            return; // no feedback loops

        _targets.Clear();
        var query = EntityQueryEnumerator<CERadioSpeakerComponent>();
        while (query.MoveNext(out var uid, out var speaker))
        {
            var sharesFrequency = false;
            foreach (var frequency in ent.Comp.Frequencies)
            {
                if (!speaker.Frequencies.Contains(frequency))
                    continue;

                sharesFrequency = true;
                break;
            }

            if (!sharesFrequency)
                continue;

            if (!_power.IsPowered(uid))
                continue;

            if (!_zLevels.TryGetEffectiveDistance(ent, uid, out var distance) || distance > ent.Comp.Radius)
                continue;

            if (!_recentlySent.Add((uid, args.Source, args.Message)))
                continue;

            _targets.Add(uid);
        }

        if (_targets.Count == 0)
            return;

        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        _broadcastingSpeakers.Clear();
        _broadcastingSpeakers.UnionWith(_targets);
        _broadcastHeard.Clear();

        // Speaking spawns z-level transmitters and raises more speech events, so do it outside the query.
        foreach (var speaker in _targets.ToArray())
        {
            _chat.TrySendInGameICMessage(speaker,
                args.Message,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                nameOverride: nameEv.VoiceName,
                checkRadioPrefix: false,
                ignoreActionBlocker: true);

            ProtoId<SpeechVerbPrototype>? verb = null;
            if (TryComp<SpeechComponent>(speaker, out var speech))
                verb = speech.SpeechVerb;

            _zSpeaking.TransmitToAdjacentZLevels(speaker, args.Message, nameEv.VoiceName, InGameICChatType.Speak, verb);
        }

        _broadcastingSpeakers.Clear();
        _broadcastHeard.Clear();
    }

    [SubscribeLocalEvent]
    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        if (!_broadcastingSpeakers.Contains(ev.Source))
            return;

        // Every speaker still shows its bubble, but each listener gets the phrase in the chat log only once.
        _alreadyHeard.Clear();
        foreach (var session in ev.Recipients.Keys)
        {
            if (!_broadcastHeard.Add(session))
                _alreadyHeard.Add(session);
        }

        foreach (var session in _alreadyHeard)
        {
            ev.Recipients[session] = ev.Recipients[session] with { HideChatOverride = true };
        }
    }
}
