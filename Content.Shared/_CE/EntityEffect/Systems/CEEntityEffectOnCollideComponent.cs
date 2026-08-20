using Robust.Shared.GameStates;

namespace Content.Shared._CE.EntityEffect;

/// <summary>
/// Applies a list of <see cref="CEEntityEffect"/> to entities that collide with this entity.
/// Handled server-side by <c>CEEntityEffectOnCollideSystem</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEEntityEffectOnCollideComponent : Component
{
    [DataField]
    public string TriggerFixtureId = "fix1";

    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    /// <summary>
    /// Whether to apply effects to airborne entities (BodyStatus.InAir).
    /// Set to false for ground-level hazards like spikes or lava that can be jumped over.
    /// </summary>
    [DataField]
    public bool AffectAirborne = false;
}
