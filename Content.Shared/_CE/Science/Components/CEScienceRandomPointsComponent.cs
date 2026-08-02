namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks a <see cref="CEScientificInterestComponent"/>-bearing entity as having its research
/// points randomized rather than fixed in yaml. On <see cref="Robust.Shared.GameObjects.MapInitEvent"/>,
/// rolls 3 essence types (weighted towards low tiers, may repeat - see
/// <see cref="Content.Shared._CE.MagicEssence.Systems.CEMagicEssenceSystem.GetRandomEssenceType"/>),
/// then distributes a random total point budget between <see cref="MinAmount"/> and
/// <see cref="MaxAmount"/> across those 3 types 70%/20%/10% - the same weighting a magic essence
/// node uses for its own 3 rolled aspects - and writes the result into the entity's
/// <see cref="CEScientificInterestComponent.Points"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEScienceRandomPointsComponent : Component
{
    [DataField]
    public int MinAmount = 3;

    [DataField]
    public int MaxAmount = 30;
}
