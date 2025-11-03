using Robust.Shared.GameStates;

namespace Content.Shared._CE.Recycler;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CESharedRecyclerSystem))]
public sealed partial class CERecyclerComponent : Component
{
}
