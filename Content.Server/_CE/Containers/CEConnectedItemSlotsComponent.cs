namespace Content.Server._CE.Containers;

/// <summary>
/// Marks an ordinary item-slot fixture as part of a cardinally connected insertion group.
/// </summary>
[RegisterComponent]
public sealed partial class CEConnectedItemSlotsComponent : Component
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
