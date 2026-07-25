using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Events;

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know its thaumaturgical
/// essence composition. Mirrors <see cref="Content.Shared.Cargo.PriceCalculationEvent"/>.
/// </summary>
[ByRefEvent]
public record struct CEMagicEssenceCalculationEvent()
{
    /// <summary>
    /// The essence composition accumulated so far.
    /// </summary>
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Essences = new();

    /// <summary>
    /// Whether this event was already handled.
    /// </summary>
    public bool Handled = false;
}
