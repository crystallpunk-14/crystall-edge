namespace Content.Shared._CE.InfusionAltar.Components;

/// <summary>
/// Marks an item as an infusion altar stabilizer. Placed loose on the ground within an altar's
/// <see cref="CEInfusionAltarComponent.StabilizerScanRadius"/> (not on a sub-pedestal), a stabilizer
/// reduces the altar's instability growth rate if a matching stabilizer sits at the antipodal tile
/// offset; unpaired stabilizing power instead slightly raises it.
/// </summary>
[RegisterComponent]
public sealed partial class CEInfusionAltarStabilizerComponent : Component
{
    [DataField]
    public float StabilizingPower = 1f;
}
