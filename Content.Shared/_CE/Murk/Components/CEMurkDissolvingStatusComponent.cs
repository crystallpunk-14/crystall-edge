using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Holds the current dissolution level and which presentation effects react to it. Standalone from
/// <see cref="CEMurkDissolvingComponent"/> so an entity can be spawned already dissolved (e.g. a
/// murk shadow mob prototyped with <see cref="Dissolved"/> at 1) without running the murk-tracking
/// process that grows and shrinks it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEMurkDissolvingStatusComponent : Component
{
    /// <summary>
    /// Current dissolution level, 0 (not dissolved) to 1 (fully dissolved).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Dissolved;

    /// <summary>
    /// Whether the client-side desaturation overlay reacts to <see cref="Dissolved"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OverlayEnabled = true;

    /// <summary>
    /// Whether the murk ambience layers react to <see cref="Dissolved"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SoundEnabled = true;

    /// <summary>
    /// Whether the dissolve sprite shader reacts to <see cref="Dissolved"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShaderEnabled = true;

    /// <summary>
    /// Alert shown while <see cref="Dissolved"/> is above zero, with severity scaled to it.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "CEMurkDissolving";
}
