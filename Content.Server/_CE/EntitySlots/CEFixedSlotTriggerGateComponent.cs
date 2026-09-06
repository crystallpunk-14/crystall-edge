using Content.Shared.Whitelist;

namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Runs an entity's timer trigger only while the entity occupies an allowed fixed-slot host.
/// </summary>
[RegisterComponent]
public sealed partial class CEFixedSlotTriggerGateComponent : Component
{
    /// <summary>
    /// Fixed-slot hosts that are allowed to run this entity's timer.
    /// This prototype-authored policy is expected to remain immutable while inserted.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist HostWhitelist = new();
}
