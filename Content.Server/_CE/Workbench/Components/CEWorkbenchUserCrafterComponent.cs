namespace Content.Server._CE.Workbench;

/// <summary>
/// This workbench can only operate when there is a user interacting with it.
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchUserCrafterComponent : Component
{
}
