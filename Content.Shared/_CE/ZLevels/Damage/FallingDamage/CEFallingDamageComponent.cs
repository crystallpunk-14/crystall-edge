/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Health;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Damage.FallingDamage;

/// <summary>
/// Additional damage when falling on this entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEFallingDamageComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public CEDamageSpecifier Damage = new();
}
