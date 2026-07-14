using Content.Shared._CE.Speech;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.Speech;

/// <summary>
/// Client-side bark speech system. Provides <see cref="PlayPreview"/> for the
/// character editor to audition bark voice settings without a server round-trip.
/// </summary>
public sealed partial class CEBarkSpeechSystem : CESharedBarkSpeechSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private PreviewSequence? _preview;

    /// <summary>
    /// Sample phrases used for bark voice preview, picked at random.
    /// </summary>
    private static readonly string[] SamplePhrases =
    {
        "Бутерброд? мне все рассказал!",
        "Ты видишь Луркера? И я не вижу. А он есть!",
        "Нету ручек - нету зелья!",
        "Однажды и меня вела дорога приключений?",
        "Я бы на нем курочку отжарила бы!",
        "ЕБАТЬ ТАРЕЛКИ!",
    };

    /// <summary>
    /// Plays a bark preview using the given voice profile and pitch.
    /// Stops any currently playing preview first.
    /// </summary>
    public void PlayPreview(ProtoId<CEBarkSpeechPrototype> voiceId, float basePitch)
    {
        StopPreview();

        if (!_proto.TryIndex(voiceId, out var profile))
            return;

        var phrase = SamplePhrases[new Random().Next(SamplePhrases.Length)];
        var syllables = BuildSyllables(phrase, basePitch, profile);
        if (syllables.Count == 0)
            return;

        _preview = new PreviewSequence
        {
            Syllables = syllables,
            NextIndex = 0,
            NextPlayTime = _timing.CurTime,
        };
    }

    /// <summary>
    /// Stops any currently playing bark preview.
    /// </summary>
    public void StopPreview()
    {
        _preview = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_preview == null)
            return;

        var now = _timing.CurTime;

        while (_preview.NextIndex < _preview.Syllables.Count && now >= _preview.NextPlayTime)
        {
            var syllable = _preview.Syllables[_preview.NextIndex];

            if (!syllable.IsPause)
            {
                var audioParams = syllable.AudioParams
                    .WithPitchScale(syllable.Pitch)
                    .WithVolume(syllable.AudioParams.Volume + syllable.VolumeBoost);

                _audio.PlayGlobal(syllable.Sound, Filter.Local(), false, audioParams);
            }

            _preview.NextIndex++;

            if (_preview.NextIndex < _preview.Syllables.Count)
                _preview.NextPlayTime = now + TimeSpan.FromSeconds(syllable.Duration);
        }

        if (_preview.NextIndex >= _preview.Syllables.Count)
            _preview = null;
    }

    private sealed partial class PreviewSequence
    {
        public List<BarkSyllable> Syllables = new();
        public int NextIndex;
        public TimeSpan NextPlayTime;
    }
}
