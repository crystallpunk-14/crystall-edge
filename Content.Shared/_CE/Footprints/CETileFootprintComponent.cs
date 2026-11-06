namespace Content.Shared._CE.Footprints;

/// <summary>
/// Temporary footprint pressed into a soft tile (snow, sand). Has no reagents and despawns on its own.
/// </summary>
[RegisterComponent]
public sealed partial class CETileFootprintComponent : Component
{
    /// <summary>
    /// Max tile footprints on one tile. A new one removes the oldest.
    /// </summary>
    [DataField]
    public int MaxPerTile = 3;
}
