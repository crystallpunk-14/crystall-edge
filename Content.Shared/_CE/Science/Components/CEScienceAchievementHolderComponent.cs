using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks an item as holding a science achievement. On MapInit, the entity's name and
/// description are overridden from <see cref="CEScienceAchievementPrototype"/>.
/// Using the item in-hand starts a do-after that applies the achievement's effects to the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEScienceAchievementHolderComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<CEScienceAchievementPrototype> Achievement;
}
