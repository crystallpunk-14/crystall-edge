namespace Content.Server._CE.Power.Components;

/// <summary>
/// Allows you to enable and disable the connection of all CableNodes of this entity through interaction in the world.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEDelayedConnectorComponent : Component
{
    [DataField]
    public bool Active = true;

    /// <summary>
    /// Allows players change SelectedDelay via verbs menu, from AvailableDelays options.
    /// </summary>
    [DataField]
    public bool Configurable = true;

    [DataField]
    public TimeSpan SelectedDelay = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public List<TimeSpan> AvailableDelays = new()
    {
        TimeSpan.FromSeconds(0.5f),
        TimeSpan.FromSeconds(1f),
        TimeSpan.FromSeconds(1.5f),
        TimeSpan.FromSeconds(2f),
    };

    [DataField, AutoPausedField]
    public TimeSpan NextChangeTime = TimeSpan.Zero;
}
