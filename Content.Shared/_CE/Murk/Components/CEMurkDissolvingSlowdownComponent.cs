using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Applies a movement speed penalty scaled to <see cref="CEMurkDissolvingStatusComponent.Dissolved"/>.
/// Requires a <see cref="CEMurkDissolvingStatusComponent"/> on the same entity to have any effect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEMurkDissolvingSlowdownComponent : Component
{
    /// <summary>
    /// Movement speed penalty applied at full dissolution (Dissolved == 1). E.g. 0.5 means the
    /// entity moves at 50% speed when fully dissolved. Scales linearly with Dissolved in between.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxSlowdown = 0.9f;
}
