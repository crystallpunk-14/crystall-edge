using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    private bool _allowFlavorText;

    // Dnd: flavor-text menu is disabled. The editor below stays so it can be turned back on.
    private const bool FlavorTextMenuEnabled = false;

    private FlavorText.FlavorText? _flavorText;
    private TextEdit? _flavorTextEdit;

    /// <summary>
    /// Refreshes the flavor text editor status.
    /// </summary>
    public void RefreshFlavorText()
    {
        if (FlavorTextMenuEnabled && _allowFlavorText)
        {
            if (_flavorText != null)
                return;

            _flavorText = new FlavorText.FlavorText();
            TabContainer.AddChild(_flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));
            _flavorTextEdit = _flavorText.CFlavorTextInput;

            _flavorText.OnFlavorTextChanged += OnFlavorTextChange;
        }
        else
        {
            if (_flavorText == null)
                return;

            TabContainer.RemoveChild(_flavorText);
            _flavorText.OnFlavorTextChanged -= OnFlavorTextChange;
            _flavorText.Dispose();
            _flavorTextEdit?.Dispose();
            _flavorTextEdit = null;
            _flavorText = null;
        }
    }

    private void OnFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithFlavorText(content);
        SetDirty();
    }

    private void UpdateFlavorTextEdit()
    {
        if (_flavorTextEdit != null)
        {
            _flavorTextEdit.TextRope = new Rope.Leaf(Profile?.FlavorText ?? "");
        }
    }
}
