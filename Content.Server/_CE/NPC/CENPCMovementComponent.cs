namespace Content.Server._CE.NPC;

/// <summary>Opt-in walking for NPCs. Urgent actions temporarily select sprinting.</summary>
[RegisterComponent]
public sealed partial class CENPCMovementComponent : Component
{
    [DataField]
    public bool Walking = true;
}
