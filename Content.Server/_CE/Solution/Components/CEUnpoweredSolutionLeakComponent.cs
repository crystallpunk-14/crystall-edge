using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server._CE.Solution.Components;

/// <summary>
/// While the owning entity is unpowered, slowly spills the named solution onto the floor beneath it.
/// Requires an ApcPowerReceiverComponent on the same entity to ever be considered unpowered.
/// </summary>
[RegisterComponent]
public sealed partial class CEUnpoweredSolutionLeakComponent : Component
{
    [DataField]
    public string SolutionName = "essence";

    /// <summary>
    /// How many units leak out per second while unpowered.
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
