using System.Linq;
using Content.Shared._CE.Traits;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    /// <summary>
    /// Whether the picked traits can be saved: balanced to zero and free of conflicts.
    /// </summary>
    public bool TraitsValid => Profile is null ||
                               CETraitRules.IsSelectionValid(Profile.TraitPreferences.ToList(), _prototypeManager);

    /// <summary>
    /// Rebuilds the trait grid - on profile change, trait prototype reload or unlock (achievement) updates.
    /// </summary>
    public void RefreshTraits()
    {
        TabContainer.SetTabTitle(2, Loc.GetString("humanoid-profile-editor-traits-tab"));
        TraitPicker.Populate(Profile);
        UpdateSaveButton();
    }

    private void OnTraitToggled(ProtoId<CETraitPrototype> trait, bool pref)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithTraitPreference(trait, pref);
        TraitPicker.SetSelection(Profile.TraitPreferences);
        SetDirty();
        UpdateSaveButton();
    }
}
