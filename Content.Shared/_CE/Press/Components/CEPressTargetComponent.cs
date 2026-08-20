namespace Content.Shared._CE.Press.Components;

/// <summary>
/// Marker for entities that act as a "target platform" under a CEPress. When present on an
/// anchored entity found on the press's tile, the press raises CEPressCrushingTargetEvent on it
/// instead of applying damage directly. The target entity must be anchored to be recognized —
/// an unanchored entity with this component is treated as a normal scanned entity instead.
/// </summary>
[RegisterComponent]
public sealed partial class CEPressTargetComponent : Component
{
}
