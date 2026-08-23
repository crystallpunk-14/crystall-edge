namespace Content.Server._CE.GOAP.Classifiers;

/// <summary>
/// Per-agent cache of classified knowledge. Maintained by <see cref="CEGOAPKnowledgeCacheSystem"/>
/// in response to <see cref="CEGOAPKnowledgeUpdatedEvent"/>. Buckets entities from
/// <see cref="Content.Shared._CE.GOAP.Components.CEGOAPComponent.Knowledge"/> by faction relation
/// so consumers (sensors, selectors) don't have to rerun the predicates each tick.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPKnowledgeCacheComponent : Component
{
    /// <summary>Known entities classified as hostile relative to this agent.</summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Enemies = new();

    /// <summary>Known entities classified as friendly relative to this agent.</summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Allies = new();
}
