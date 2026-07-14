using Robust.Shared.GameStates;

namespace Content.Shared._CE.EntityEffect.Systems;

/// <summary>
/// Applies effects from <see cref="CEEntityEffect"/> list at the destruction point
/// via <see cref="CEDestructedEvent"/> (raised by <see cref="CEDestructibleSystem"/>).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEDestructionEffectComponent : Component
{
    /// <summary>
    /// Effects applied at the destruction point.
    /// Use <see cref="Effects.AreaEffect"/> to target entities around the point.
    /// </summary>
    [DataField]
    public List<CEEntityEffect> Effects = new();
}
