using System.Linq;
using Content.Client._CE.Speech;
using Content.Shared._CE.Speech;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<CEBarkSpeechPrototype> _barkVoices = new();
    private CEBarkSpeechSystem? _barkSystem;

    private void RefreshBarkVoices()
    {
        BarkVoiceButton.Clear();
        _barkVoices.Clear();

        _barkVoices.AddRange(_prototypeManager.EnumeratePrototypes<CEBarkSpeechPrototype>().Where(p => p.Selectable));
        _barkVoices.Sort((a, b) => string.Compare(a.ID, b.ID, StringComparison.Ordinal));

        for (var i = 0; i < _barkVoices.Count; i++)
        {
            BarkVoiceButton.AddItem(_barkVoices[i].ID, i);

            if (Profile?.BarkVoice == _barkVoices[i].ID)
                BarkVoiceButton.SelectId(i);
        }
    }

    private void UpdateBarkControls()
    {
        if (Profile == null)
            return;

        for (var i = 0; i < _barkVoices.Count; i++)
        {
            if (_barkVoices[i].ID == Profile.BarkVoice)
            {
                BarkVoiceButton.SelectId(i);
                break;
            }
        }

        // CrystallEdge: map stored pitch [MinPitchScale, MaxPitchScale] -> slider [0, 100]
        var normalized = (Profile.BarkPitch - CESharedBarkSpeechSystem.MinPitchScale)
            / (CESharedBarkSpeechSystem.MaxPitchScale - CESharedBarkSpeechSystem.MinPitchScale);
        BarkPitchSlider.Value = Math.Clamp(normalized * 100f, 0f, 100f);
        // CrystallEdge end
    }

    private void SetBarkVoice(string id)
    {
        Profile = Profile?.WithBarkVoice(id);
        SetDirty();
    }

    private void SetBarkPitch(float normalizedValue)
    {
        // CrystallEdge: normalizedValue is [0, 1]; map to stored pitch [MinPitchScale, MaxPitchScale]
        var actualPitch = CESharedBarkSpeechSystem.MinPitchScale
            + normalizedValue * (CESharedBarkSpeechSystem.MaxPitchScale - CESharedBarkSpeechSystem.MinPitchScale);
        Profile = Profile?.WithBarkPitch(actualPitch);
        // CrystallEdge end
        SetDirty();
    }

    private void OnBarkPreviewPressed()
    {
        if (Profile == null)
            return;

        _barkSystem ??= _entManager.System<CEBarkSpeechSystem>();
        _barkSystem.PlayPreview(Profile.BarkVoice, Profile.BarkPitch);
    }
}
