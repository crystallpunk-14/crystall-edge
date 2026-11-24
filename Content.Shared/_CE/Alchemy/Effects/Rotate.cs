namespace Content.Shared._CE.Alchemy.Effects;

/// <summary>
/// Modifier: rotates the accumulated marker position around the center of the map.
/// </summary>
public sealed partial class Rotate : CEAlchemyEffect
{
    /// <summary>
    /// Degrees per unit of the reagent; positive is clockwise, negative is counterclockwise.
    /// </summary>
    [DataField(required: true)]
    public float DegreesPerUnit;
}
