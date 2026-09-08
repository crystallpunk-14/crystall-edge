using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.EnergyExtractor;

/// <summary>
/// A power generator that periodically destroys items sitting in its internal grid storage,
/// converting each one into battery charge based on that item's <see cref="CEEnergyExtractableComponent"/>.
/// Items are usually delivered by a funnel fed from a conveyor belt.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CEEnergyExtracterComponent : Component
{
    /// <summary>
    /// Id of the container that holds items waiting to be processed.
    /// Matches the funnel's target container so items can be funneled straight in.
    /// </summary>
    [DataField]
    public string ContainerId = "storagebase";

    /// <summary>
    /// How often a single stored item is destroyed and turned into energy.
    /// </summary>
    [DataField]
    public TimeSpan ProcessFrequency = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time a stored item will be processed.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextProcessTime = TimeSpan.Zero;

    /// <summary>
    /// Optional whitelist for what stored items the extractor is allowed to destroy.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Optional blacklist for what stored items the extractor must never destroy.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Sound played when an item is destroyed and converted into energy.
    /// </summary>
    [DataField]
    public SoundSpecifier? ProcessSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
}
