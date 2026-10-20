using Content.Shared._CE.EntityEffect;

namespace Content.Shared._CE.RestorationRitual.Components;

/// <summary>
/// A pedestal near the Lucson Sphere holding a lightheart shard for the Restoration Ritual.
/// An inserted shard is locked in for the round; a shard whose index is already on another pedestal of the map
/// is thrown back out after <see cref="ImpactDelay"/>. The ritual checks every pedestal on the sphere's map.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CERitualPedestalComponent : Component
{
    /// <summary>
    /// Item slot the shard is placed into.
    /// </summary>
    [DataField]
    public string Slot = "pedestal";

    /// <summary>
    /// Time from insertion until the shard is accepted or thrown back out.
    /// </summary>
    [DataField]
    public TimeSpan ImpactDelay = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Speed a rejected shard is thrown away with.
    /// </summary>
    [DataField]
    public float RejectSpeed = 4f;

    /// <summary>
    /// Applied to the pedestal as soon as a fitting shard is inserted.
    /// </summary>
    [DataField]
    public List<CEEntityEffect> InsertEffects = new();

    /// <summary>
    /// Applied to the pedestal when a fitting shard is accepted.
    /// </summary>
    [DataField]
    public List<CEEntityEffect> AcceptEffects = new();

    /// <summary>
    /// Applied to the pedestal when a duplicate shard is thrown back out.
    /// </summary>
    [DataField]
    public List<CEEntityEffect> RejectEffects = new();

    /// <summary>
    /// Index of the inserted shard, set on insertion: pending and accepted shards both block duplicates.
    /// </summary>
    [DataField]
    public int? ShardIndex;

    /// <summary>
    /// Whether the inserted shard is accepted and counts towards the ritual.
    /// </summary>
    [DataField]
    public bool Accepted;

    /// <summary>
    /// Whether the inserted shard is a duplicate waiting to be thrown back out.
    /// </summary>
    [DataField]
    public bool Rejecting;

    /// <summary>
    /// When the inserted shard is accepted or thrown back out.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan? ImpactTime;
}
