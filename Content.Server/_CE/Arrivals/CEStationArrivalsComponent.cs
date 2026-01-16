using Robust.Shared.Utility;

namespace Content.Server._CE.Arrivals;

/// <summary>
/// Added to a station that is available for arrivals shuttles.
/// </summary>
[RegisterComponent, Access(typeof(CEArrivalsSystem))]
public sealed partial class CEStationArrivalsComponent : Component
{
    [DataField]
    public EntityUid? Shuttle;

    [DataField] public ResPath ShuttlePath = new("/Maps/_CE/Shuttles/arrivals.yml"); //TODO
}
