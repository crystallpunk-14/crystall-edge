using Robust.Shared.GameStates;

namespace Content.Shared._CE.GOAP.Components;

/// <summary>
/// Added to entities that are currently classified as an enemy by some GOAP agent's
/// knowledge cache. Used for event-based target sensors (mob state changed) and to let
/// clients detect when the local player is being hunted.
/// Automatically managed by <see cref="CEGOAPKnowledgeCacheComponent"/> classifier.
/// Networked so clients can react.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEGOAPTargetComponent : Component
{
    /// <summary>
    /// Set of GOAP agent UIDs that have this entity in their <see cref="CEGOAPKnowledgeCacheComponent.Enemies"/>.
    /// Server-side only — not networked.
    /// </summary>
    [NonSerialized]
    public HashSet<EntityUid> Trackers = new();
}
