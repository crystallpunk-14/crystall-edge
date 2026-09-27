using Robust.Shared.Serialization;

namespace Content.Shared._CE.Roundflow;

/// <summary>
/// Periodically broadcast by the server while the murk consuming rule is active. Drives the
/// round progress bar at the top of the screen.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERoundProgressStateEvent(bool visible, float light, float murk) : EntityEventArgs
{
    /// <summary>
    /// Whether the bar should be shown at all (only once the Lucson Sphere has cracked).
    /// </summary>
    public readonly bool Visible = visible;

    /// <summary>
    /// Light Monolith charge, 0..1.
    /// </summary>
    public readonly float Light = light;

    /// <summary>
    /// How much of the time until the sphere collapses has passed, 0..1.
    /// </summary>
    public readonly float Murk = murk;
}
