using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Fluids;

/// <summary>
/// Under the open sky every reagent of the solution evaporates, not only ones with an evaporation speed.
/// Works with any entity holding a solution: puddles, footprints, etc.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEOutdoorEvaporationComponent : Component
{
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// Evaporated volume per second.
    /// </summary>
    [DataField]
    public FixedPoint2 Speed = FixedPoint2.New(0.1f);

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Delete the entity once its solution is empty. Puddles delete themselves, so they keep it false.
    /// </summary>
    [DataField]
    public bool DeleteOnEmpty;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;
}
