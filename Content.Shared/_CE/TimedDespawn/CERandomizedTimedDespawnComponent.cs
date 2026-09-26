using Robust.Shared.GameStates;

namespace Content.Shared._CE.TimedDespawn;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class CERandomizedTimedDespawnComponent : Component
{
    /// <summary>
    /// Lower bound for the random <see cref="Lifetime"/> rolled on spawn.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan MinLifetime;

    /// <summary>
    /// Upper bound for the random <see cref="Lifetime"/> rolled on spawn.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan MaxLifetime;

    /// <summary>
    /// When the entity was spawned, i.e. when <see cref="Lifetime"/> started counting down.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan SpawnTime;

    /// <summary>
    /// Total lifetime rolled for this entity on spawn, randomized between <see cref="MinLifetime"/>
    /// and <see cref="MaxLifetime"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Lifetime;
}
