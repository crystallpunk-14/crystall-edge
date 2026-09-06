namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Marks a fixed-slot host as part of a cardinally connected storage group.
/// </summary>
[RegisterComponent]
public sealed partial class CEConnectedEntitySlotsComponent : Component
{
    /// <summary>
    /// Only cardinally adjacent hosts with the same group belong to one network.
    /// </summary>
    [DataField(required: true)]
    public string Group = string.Empty;

    /// <summary>
    /// The node name in <see cref="Content.Shared.NodeContainer.NodeContainerComponent"/> used for connectivity.
    /// </summary>
    [DataField(required: true)]
    public string Node = string.Empty;
}
