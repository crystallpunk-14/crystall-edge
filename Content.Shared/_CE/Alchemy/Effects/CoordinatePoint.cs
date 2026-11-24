using System.Numerics;

namespace Content.Shared._CE.Alchemy.Effects;

/// <summary>
/// Carrier: pulls the marker towards its point on the brewing map.
/// </summary>
public sealed partial class CoordinatePoint : CEAlchemyEffect
{
    /// <summary>
    /// Base point on the map. The round seed shifts it, the shifted value lives in the round world.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Pos;

    /// <summary>
    /// Effect stock per unit of the reagent: how strong the solution gets from it.
    /// </summary>
    [DataField]
    public float Stock = 1f;
}
