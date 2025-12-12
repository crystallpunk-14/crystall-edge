namespace Content.Server._CE.Power.Components;

/// <summary>
/// Allows you to enable and disable the connection of all CableNodes of this entity through interaction in the world.
/// </summary>
[RegisterComponent]
public sealed partial class CEToggleableConnectorComponent : Component
{
    [DataField]
    public bool Active = true;
}
