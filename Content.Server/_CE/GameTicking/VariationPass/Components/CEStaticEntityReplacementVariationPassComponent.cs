using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking.VariationPass.Components;

/// <summary>
/// Variation pass component that replaces a fixed number of entities matching specified prototypes
/// with their replacement counterparts.
/// Unlike EntityReplaceVariationPass which uses probabilistic replacement based on averages,
/// this system replaces an exact number of entities specified in the configuration.
/// </summary>
[RegisterComponent]
public sealed partial class CEStaticEntityReplacementVariationPassComponent : Component
{
    /// <summary>
    /// Dictionary mapping source entity prototypes to their replacement prototypes.
    /// Key: The prototype ID of entities to replace.
    /// Value: The prototype ID of the replacement entity.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, EntProtoId> ReplacementMap = new();

    /// <summary>
    /// Exact number of entities to replace.
    /// The system will randomly select this many entities from all matching candidates.
    /// </summary>
    [DataField]
    public int ReplacementCount = 10;

    /// <summary>
    /// Optional whitelist for additional filtering of entities before replacement.
    /// Only entities that pass this whitelist will be considered for replacement.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Optional blacklist to exclude certain entities from replacement.
    /// Entities matching this blacklist will never be replaced.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
