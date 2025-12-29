namespace Content.Shared._CE.Funnel;

/// <summary>
/// Power-gated activator for nearby funnels.
/// When this entity is powered, any <see cref="CEFunnelComponent"/> anchored on the same tile
/// and facing the same direction becomes enabled to perform periodic item extraction.
/// When unpowered (or removed), those funnels are disabled.
/// </summary>
[RegisterComponent]
public sealed partial class CEFunnelActivatorComponent : Component
{
}
