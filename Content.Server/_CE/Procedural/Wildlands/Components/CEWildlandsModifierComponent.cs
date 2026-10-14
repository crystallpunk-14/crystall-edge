using Content.Server._CE.Procedural.Generation.Points;
using Content.Server._CE.Procedural.Wildlands.Effects;

namespace Content.Server._CE.Procedural.Wildlands.Components;

/// <summary>
/// Makes an entity prototype a pickable wildlands modifier; read from the prototype, never spawned.
/// </summary>
[RegisterComponent]
public sealed partial class CEWildlandsModifierComponent : Component
{
    [DataField(required: true)]
    public List<ICEWildlandsModifierEffect> Effects = new();

    /// <summary>
    /// Rolled when the modifier applies, around the config center and only inside the modifier's region, so the
    /// modifier places its own area on any map. Visible to this modifier's effects only.
    /// </summary>
    [DataField]
    public List<CEProceduralPoint> Points = new();
}
