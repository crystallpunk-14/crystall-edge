using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Marks an entity as dissolving while standing in the murk. Drives <see cref="CEMurkDissolvingStatusComponent.Dissolved"/>
/// up while the entity is inside the murk and back down to zero while it is not.
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
    /// Set once the entity has fully dissolved into a murked soul. Freezes the dissolution level
    /// for good - a soul stays dissolved forever, nothing walks it back.
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
    /// How much the dissolution level grows per second while the entity is in the murk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DissolvingSpeed = 0.02f;

    /// <summary>
    /// How much the dissolution level shrinks per second while the entity is not in the murk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RestoringSpeed = 0.01f;

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
}
