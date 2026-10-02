using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Conditions;

namespace Content.Server._CE.Power.Components;

/// <summary>
/// While this entity's power net has supply, damaging it, prying it, unanchoring it or finishing a construction step on it
/// (wrenching, installing a valve) releases <see cref="Effects"/> around it. Interactions by a user failing
/// <see cref="UserConditions"/> are cancelled.
/// </summary>
[RegisterComponent, Access(typeof(CEEnergizedInteractionSystem))]
public sealed partial class CEEnergizedInteractionComponent : Component
{
    /// <summary>
    /// Effects applied at this entity's position. Source is this entity, there is no target -
    /// use <see cref="Shared._CE.EntityEffect.Effects.AreaEffect"/> to hit entities around.
    /// </summary>
    [DataField]
    public List<CEEntityEffect> Effects = new();

    /// <summary>
    /// The interacting user must pass all of these, otherwise the interaction is cancelled.
    /// </summary>
    [DataField]
    public List<CEEntityCondition> UserConditions = new() { new Insulated() };
}
