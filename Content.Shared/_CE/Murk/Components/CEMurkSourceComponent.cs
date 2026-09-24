using Content.Shared._CE.Murk;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Edits the strength of the murk on the map within a radius around the source.
/// Projects across z-levels as a sphere, see <see cref="CESharedMurkSystem.TryProjectRadius"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedMurkSystem))]
public sealed partial class CEMurkSourceComponent : Component
{
    /// <summary>
    /// Radius in tiles, and at the same time the strength: negative dispels murk, positive thickens it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 5f;

    [DataField, AutoNetworkedField]
    public bool Active = true;
}
