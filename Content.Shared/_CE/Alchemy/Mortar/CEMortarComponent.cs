using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Alchemy.Mortar;

/// <summary>
/// A storage whose items are all ground into its solution at once when a <see cref="CEPestleComponent"/> is used on it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CEMortarSystem))]
public sealed partial class CEMortarComponent : Component
{
    [DataField]
    public string Solution = "mortar";

    [DataField]
    public string Container = StorageComponent.ContainerId;
}
