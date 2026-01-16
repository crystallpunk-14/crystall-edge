using Content.Shared.DoAfter;

namespace Content.Server._CE.Workbench;

/// <summary>
/// Enables a workbench to automatically craft the selected recipe when powered, without user interaction.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchAutoCrafterComponent : Component
{
    [DataField]
    public DoAfterId? ActiveDoAfter;

    [DataField]
    public TimeSpan CraftDelay = TimeSpan.FromSeconds(1f);

    [DataField, AutoPausedField]
    public TimeSpan NextCraftTime = TimeSpan.Zero;
}
