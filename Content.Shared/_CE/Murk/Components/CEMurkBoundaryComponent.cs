using Content.Shared._CE.Murk;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Clears a sphere in the boundary wall - a second murk layer that is not capped by
/// <c>CEMurkMaxOpacity</c> and fills the whole murked map outside every boundary. 
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedMurkSystem))]
public sealed partial class CEMurkBoundaryComponent : Component
{
    /// <summary>
    /// Radius in tiles, independent of any <see cref="CEMurkSourceComponent.Intensity"/> on the
    /// same entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 5f;

    [DataField, AutoNetworkedField]
    public bool Active = true;
}
