using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Marks an entity as dissolving while standing in the murk. Tracks a dissolution level that
/// grows while the entity is inside the murk and shrinks back to zero while it is not.
/// Once it hits 1 the entity is converted into a murked soul, which is a one way trip.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class CEMurkDissolvingComponent : Component
{
    /// <summary>
    /// Whether this entity's dissolution level should be updated at all.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Whether dissolution garbles this entity's speech.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AffectSpeech = true;

    /// <summary>
    /// Set once the entity has fully dissolved into a murked soul. Freezes <see cref="Dissolved"/>
    /// for good and stops the speech garbling - the soul speaks through its own system instead.
    /// The slowdown stays: a soul still drags itself around.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Converted;

    /// <summary>
    /// Components added to the entity when it turns into a murked soul. This is where the AI,
    /// the soul's speech and anything else the husk should wake up with lives.
    /// </summary>
    [DataField]
    public ComponentRegistry ConversionComponents = new();

    /// <summary>
    /// Components stripped from the entity when it turns into a murked soul, e.g. its ability to
    /// take damage.
    /// </summary>
    [DataField]
    public ComponentRegistry ConversionRemoveComponents = new();

    /// <summary>
    /// Current dissolution level, 0 (not dissolved) to 1 (fully dissolved).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Dissolved;

    /// <summary>
    /// How much <see cref="Dissolved"/> grows per second while the entity is in the murk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DissolvingSpeed = 0.02f;

    /// <summary>
    /// How much <see cref="Dissolved"/> shrinks per second while the entity is not in the murk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RestoringSpeed = 0.01f;

    /// <summary>
    /// Movement speed penalty applied at full dissolution (<see cref="Dissolved"/> == 1).
    /// E.g. 0.5 means the entity moves at 50% speed when fully dissolved. Scales linearly
    /// with <see cref="Dissolved"/> in between.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxSlowdown = 0.9f;

    /// <summary>
    /// How often the dissolution level is updated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time the dissolution level should be updated.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Alert shown while <see cref="Dissolved"/> is above zero, with severity scaled to it.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "CEMurkDissolving";
}
