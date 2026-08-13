using Content.Server._CE.MagicEssence.Systems;

namespace Content.Server._CE.MagicEssence.Components;

/// <summary>
/// Game rule that seeds and maintains the round's magic essence node pool.
/// </summary>
[RegisterComponent, Access(typeof(CEMagicEssenceNodeRuleSystem))]
public sealed partial class CEMagicEssenceNodeRuleComponent : Component
{
    /// <summary>
    /// How many mandatory magic essence nodes should exist on the station at once.
    /// </summary>
    [DataField]
    public int NodeCount = 5;
}
