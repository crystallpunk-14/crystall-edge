using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Shiny;

/// <summary>
/// Periodically spawns a sparkle VFX at a random position within <see cref="Radius"/>.
/// Logic runs client-side only (<see cref="CEShinySystem"/>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEShinyComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Effect;

    [DataField, AutoNetworkedField]
    public float Radius = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan MinFrequency = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxFrequency = TimeSpan.FromSeconds(5);

    /// <summary>Runtime state — not serialized or networked.</summary>
    public TimeSpan NextShinyTime;
}
