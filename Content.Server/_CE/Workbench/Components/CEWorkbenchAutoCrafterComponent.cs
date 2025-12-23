/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Workbench;

/// <summary>
///
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEAutoWorkbenchComponent : Component
{
    [DataField]
    public ProtoId<CEWorkbenchRecipePrototype>? SelectedRecipe;
}
