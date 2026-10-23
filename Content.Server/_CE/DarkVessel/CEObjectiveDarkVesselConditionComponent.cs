namespace Content.Server._CE.DarkVessel;

/// <summary>
/// Marks an objective completed once its holder moved into a new body - see
/// <see cref="CEDarkVesselSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CEDarkVesselSystem))]
public sealed partial class CEObjectiveDarkVesselConditionComponent : Component
{
    [DataField]
    public bool Done;
}
