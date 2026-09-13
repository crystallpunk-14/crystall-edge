using System.Linq;
using System.Numerics;
using Content.Client._CE.Lobby.UI.Roles;
using Content.Shared._CE.Roles;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    private List<(string, CERequirementsSelector)> _secretRolePriorities = new();

    /// <summary>
    /// Updates selected secret role priorities to the profile's.
    /// </summary>
    private void UpdateSecretRolePriorities()
    {
        foreach (var (roleId, prioritySelector) in _secretRolePriorities)
        {
            var priority = Profile?.SecretRolePriorities.GetValueOrDefault(roleId, JobPriority.Never) ?? JobPriority.Never;
            prioritySelector.Select((int)priority);
        }
    }

    /// <summary>
    /// Refreshes all secret role selectors.
    /// </summary>
    public void RefreshSecretRoles()
    {
        SecretRoleList.RemoveAllChildren();
        _secretRolePriorities.Clear();
        var firstCategory = true;

        var departments = _prototypeManager.EnumeratePrototypes<CESecretDepartmentPrototype>().ToList();
        departments.Sort((a, b) =>
        {
            var cmp = -a.Weight.CompareTo(b.Weight);
            return cmp != 0 ? cmp : string.Compare(a.ID, b.ID, StringComparison.Ordinal);
        });

        var items = new[]
        {
            ("humanoid-profile-editor-job-priority-never-button", (int) JobPriority.Never),
            ("humanoid-profile-editor-job-priority-low-button", (int) JobPriority.Low),
            ("humanoid-profile-editor-job-priority-medium-button", (int) JobPriority.Medium),
            ("humanoid-profile-editor-job-priority-high-button", (int) JobPriority.High),
        };

        foreach (var department in departments)
        {
            var departmentName = Loc.GetString(department.Name);

            var category = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = department.ID,
                ToolTip = Loc.GetString(department.Description),
            };

            if (firstCategory)
            {
                firstCategory = false;
            }
            else
            {
                category.AddChild(new Control
                {
                    MinSize = new Vector2(0, 23),
                });
            }

            category.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                Children =
                {
                    new Label
                    {
                        Text = departmentName,
                        HorizontalAlignment = HAlignment.Center,
                        HorizontalExpand = true,
                    }
                }
            });

            SecretRoleList.AddChild(category);

            var roles = department.Roles
                .Select(roleId => _prototypeManager.TryIndex(roleId, out var role) ? role : null)
                .Where(role => role != null)
                .Select(role => role!)
                .ToArray();

            Array.Sort(roles, (a, b) => string.Compare(a.LocalizedName, b.LocalizedName, StringComparison.CurrentCultureIgnoreCase));

            foreach (var role in roles)
            {
                var roleContainer = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                };

                var selector = new CERequirementsSelector()
                {
                    Margin = new Thickness(3f, 3f, 3f, 0f),
                    HorizontalExpand = true,
                };

                var icon = new TextureRect
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center
                };
                var roleIcon = _prototypeManager.Index(role.Icon);
                icon.Texture = _sprite.Frame0(roleIcon.Icon);
                selector.Setup(items, role.LocalizedName, 90, role.LocalizedDescription, icon);

                if (!_requirements.IsAllowed(role, (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter, out var reason))
                {
                    selector.LockRequirements(reason);
                }
                else
                {
                    selector.UnlockRequirements();
                }

                selector.OnSelected += selectedPrio =>
                {
                    var selectedRolePrio = (JobPriority)selectedPrio;
                    Profile = Profile?.WithSecretRolePriority(role.ID, selectedRolePrio);

                    foreach (var (otherId, other) in _secretRolePriorities)
                    {
                        if (otherId == role.ID)
                        {
                            other.Select(selectedPrio);
                            continue;
                        }

                        if (selectedRolePrio != JobPriority.High || (JobPriority)other.Selected != JobPriority.High)
                            continue;

                        // Lower any other high priorities to medium.
                        other.Select((int)JobPriority.Medium);
                        Profile = Profile?.WithSecretRolePriority(otherId, JobPriority.Medium);
                    }

                    UpdateSecretRolePriorities();
                    SetDirty();
                };

                // Loadout support for secret roles is not implemented yet - keep the button visible for layout parity with jobs, but disabled.
                var loadoutWindowBtn = new Button()
                {
                    ToolTip = Loc.GetString("loadout-window"),
                    Disabled = true,
                    HorizontalAlignment = HAlignment.Right,
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(3f, 3f, 0f, 0f),
                };
                loadoutWindowBtn.AddChild(new TextureRect
                {
                    TexturePath = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png",
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    Stretch = TextureRect.StretchMode.Scale,
                    SetSize = new Vector2(20, 20),
                });

                _secretRolePriorities.Add((role.ID, selector));
                roleContainer.AddChild(selector);
                roleContainer.AddChild(loadoutWindowBtn);
                category.AddChild(roleContainer);
            }
        }

        UpdateSecretRolePriorities();
    }
}
