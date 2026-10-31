using Content.Shared.FixedPoint;

namespace Content.Shared._CE.Fluids;

/// <summary>
/// Under the open sky this puddle evaporates every reagent, not only ones with an evaporation speed.
/// </summary>
[RegisterComponent]
public sealed partial class CEOutdoorEvaporationComponent : Component
{
    /// <summary>
    /// Minimal evaporation speed of any reagent in the puddle under the open sky.
    /// </summary>
    [DataField]
    public FixedPoint2 Speed = FixedPoint2.New(0.1f);
}
