using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vampire.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(CESharedVampireSystem))]
public sealed partial class CEVampireEssenceHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Essence = 1f;
}
