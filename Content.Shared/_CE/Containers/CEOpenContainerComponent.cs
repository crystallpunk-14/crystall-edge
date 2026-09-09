using Robust.Shared.GameStates;

namespace Content.Shared._CE.Containers;

/// <summary>Allows direct interaction with the named containers of an exposed world fixture.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEOpenContainerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<string> Containers = new();
}
