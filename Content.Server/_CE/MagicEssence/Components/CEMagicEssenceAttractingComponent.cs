namespace Content.Server._CE.MagicEssence.Components;

/// <summary>
/// Marker that makes floating essence entities target this entity via their <c>ChasingWalk</c>
/// component. Added to a <see cref="CEMagicEssenceAttractorComponent"/> entity while it is powered
/// (removing it on power loss stops the pull), or kept for a hungry node's entire lifetime.
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicEssenceAttractingComponent : Component;
