using Content.Shared._CE.Speech;
using Content.Shared.Chat;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CE.Speech;

/// <summary>
/// Server-side bark speech: listens to <see cref="EntitySpokeEvent"/> and plays
/// syllable sequences via <see cref="SharedAudioSystem.PlayPvs"/>.
/// </summary>
public sealed partial class CEBarkSpeechSystem : CESharedBarkSpeechSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly Dictionary<EntityUid, BarkSequence> _activeBarks = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEBarkSpeechComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    /// <summary>
    /// Volume reduction in dB applied to bark sounds when whispering.
    /// </summary>
    private const float WhisperVolumeReduction = -3f;

    /// <summary>
    /// Pitch reduction applied to bark sounds when whispering.
    /// </summary>
    private const float WhisperPitchReduction = -0.3f;

    private void OnEntitySpoke(EntityUid uid, CEBarkSpeechComponent comp, EntitySpokeEvent args)
    {
        var message = StripMarkup(args.Message);
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!_proto.TryIndex(comp.BarkSpeech, out var profile))
            return;

        var isWhisper = args.ObfuscatedMessage != null;

        var syllables = BuildSyllables(message, comp.BasePitch, profile);
        if (syllables.Count == 0)
            return;

        // Reduce volume and pitch for whispers so they sound distinctly quieter and lower.
        if (isWhisper)
        {
            for (var i = 0; i < syllables.Count; i++)
            {
                var s = syllables[i];
                if (!s.IsPause)
                {
                    s.AudioParams = s.AudioParams.WithVolume(s.AudioParams.Volume + WhisperVolumeReduction);
                    s.Pitch += WhisperPitchReduction;
                    syllables[i] = s;
                }
            }
        }

        _activeBarks[uid] = new BarkSequence
        {
            Syllables = syllables,
            NextIndex = 0,
            NextPlayTime = _timing.CurTime,
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeBarks.Count == 0)
            return;

        var now = _timing.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (uid, seq) in _activeBarks)
        {
            if (!Exists(uid))
            {
                toRemove.Add(uid);
                continue;
            }

            while (seq.NextIndex < seq.Syllables.Count && now >= seq.NextPlayTime)
            {
                var syllable = seq.Syllables[seq.NextIndex];

                if (!syllable.IsPause)
                {
                    var audioParams = syllable.AudioParams
                        .WithPitchScale(syllable.Pitch)
                        .WithVolume(syllable.AudioParams.Volume + syllable.VolumeBoost);

                    _audio.PlayPvs(syllable.Sound, uid, audioParams);
                }

                seq.NextIndex++;

                if (seq.NextIndex < seq.Syllables.Count)
                    seq.NextPlayTime = now + TimeSpan.FromSeconds(syllable.Duration);
            }

            if (seq.NextIndex >= seq.Syllables.Count)
                toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            _activeBarks.Remove(uid);
        }
    }

    private sealed partial class BarkSequence
    {
        public List<BarkSyllable> Syllables = new();
        public int NextIndex;
        public TimeSpan NextPlayTime;
    }
}
