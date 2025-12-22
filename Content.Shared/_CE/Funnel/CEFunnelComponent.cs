using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared._CE.Funnel;

/// <summary>
/// Automatically collects items that collide with it and inserts them into a storage container
/// located one tile in the direction the receiver is facing (default: north).
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEFunnelComponent : Component
{
    /// <summary>
    /// The ID of the fixture used to detect colliding items.
    /// </summary>
    [DataField]
    public string FixtureId = "collect";

    /// <summary>
    /// A whitelist for entities that can be collected by this receiver.
    /// If specified, only entities matching this whitelist will be collected.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist for entities that cannot be collected by this receiver.
    /// Entities matching this blacklist will be ignored even if they match the whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The sound played when an item is successfully inserted into storage.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound;

    /// <summary>
    /// The sound played when an item is successfully inserted into storage.
    /// </summary>
    [DataField]
    public SoundSpecifier? EjectSound;

    [DataField]
    public bool ActiveExtraction;

    [DataField]
    public TimeSpan ExtractionFrequency = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextExtractionTime = TimeSpan.Zero;
}
