using Robust.Shared.GameStates;

namespace Content.Shared._CE.GOAP.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CEGOAPSleepingComponent : Component
{
    /// <summary>
    /// Radius within which a player's presence will wake this entity.
    /// </summary>
    [DataField]
    public float WakeRadius = 5f;
}
