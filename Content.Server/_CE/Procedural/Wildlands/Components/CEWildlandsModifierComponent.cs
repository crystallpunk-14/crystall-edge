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
}
