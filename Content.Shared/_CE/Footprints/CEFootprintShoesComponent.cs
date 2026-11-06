using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Footprints;

/// <summary>
/// Shoes that carry reagents picked up from puddles and leave their own footprint sprite.
/// While worn they replace the wearer's bare feet in <see cref="CEFootprintOwnerComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEFootprintShoesComponent : Component
{
    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new ResPath("_CE/Effects/Footprints/feet.rsi"), "boots");

    [DataField]
    public Solution Solution = new();

    [DataField]
    public FixedPoint2 MaxVolume = 10;
}
