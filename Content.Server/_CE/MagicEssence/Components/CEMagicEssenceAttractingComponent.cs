using Content.Server._CE.MagicEssence.Systems;

namespace Content.Server._CE.MagicEssence.Components;

/// <summary>
/// Marker added to a <see cref="CEMagicEssenceAttractorComponent"/> entity while it is powered.
/// Floating essence entities target this component type via their <c>ChasingWalk</c> component,
/// so removing it (on power loss) stops essence from being pulled toward this entity.
/// </summary>
[RegisterComponent, Access(typeof(CEMagicEssenceAttractorSystem))]
public sealed partial class CEMagicEssenceAttractingComponent : Component;
