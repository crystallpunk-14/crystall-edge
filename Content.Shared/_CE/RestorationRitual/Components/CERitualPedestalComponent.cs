namespace Content.Shared._CE.RestorationRitual.Components;

/// <summary>
/// A pedestal near the Lucson Sphere holding a lightheart shard for the Restoration Ritual.
/// The ritual checks every pedestal on the sphere's map.
/// </summary>
[RegisterComponent]
public sealed partial class CERitualPedestalComponent : Component
{
    /// <summary>
    /// Item slot the shard is placed into.
    /// </summary>
    [DataField]
    public string Slot = "pedestal";
}
