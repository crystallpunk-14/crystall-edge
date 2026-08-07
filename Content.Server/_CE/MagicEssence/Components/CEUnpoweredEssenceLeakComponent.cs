using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server._CE.MagicEssence.Components;

/// <summary>
/// While the owning entity is unpowered, slowly leaks the named solution. Each tick, one random
/// reagent currently in the solution is picked; if it's the pure liquid embodiment of some
/// <see cref="Content.Shared._CE.MagicEssence.Prototypes.CEMagicEssenceTypePrototype"/>, 1 unit of it
/// evaporates into a floating essence instead of being lost. Otherwise it's spilled onto the floor as a
/// puddle, same as before. Requires an ApcPowerReceiverComponent on the same entity to ever be
/// considered unpowered.
/// </summary>
[RegisterComponent]
public sealed partial class CEUnpoweredEssenceLeakComponent : Component
{
    [DataField]
    public string SolutionName = "essence";

    /// <summary>
    /// How many units leak out per second while unpowered, when spilled as a puddle (the randomly
    /// picked reagent has no essence to evaporate into).
    /// </summary>
    [DataField]
    public FixedPoint2 LeakRate = FixedPoint2.New(1);

    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// Next time the leak should tick.
    /// </summary>
    public TimeSpan NextLeakTime;
}
