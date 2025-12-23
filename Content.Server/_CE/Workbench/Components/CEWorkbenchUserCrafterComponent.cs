/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

namespace Content.Server._CE.Workbench;

/// <summary>
/// This workbench can only operate when there is a user interacting with it.
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchUserCrafterComponent : Component
{
}
