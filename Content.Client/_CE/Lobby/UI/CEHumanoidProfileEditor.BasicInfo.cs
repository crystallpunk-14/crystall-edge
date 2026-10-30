
using Content.Client.Lobby.UI;
using Content.Shared.Preferences;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        SetDirty();

        if (!IsDirty)
            return;

        SpriteView.SetName(newName);
    }

    private void UpdateNameEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    /// <summary>
    /// Randomize values selectively while respecting locked values.
    /// </summary>
    private void RandomizeProfile()
    {
        var locked = GetLockedSpecies();
        if (Profile == null)
        {
            Profile = HumanoidCharacterProfile.Random(locked);
        }
        else
        {
            // Roll the species here so locked ones are skipped, then randomize the rest around it.
            var cfg = RandomizeLockButton.RandomizeCfg;
            var baseProfile = Profile;
            if ((cfg & HumanoidCharacterProfile.RandomizeCfg.Species) != 0)
            {
                baseProfile = baseProfile.WithSpecies(HumanoidCharacterProfile.RandomSpecies(locked).ID);
                cfg &= ~HumanoidCharacterProfile.RandomizeCfg.Species;
            }

            Profile = HumanoidCharacterProfile.Random(cfg, baseProfile);
        }
        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }
}
