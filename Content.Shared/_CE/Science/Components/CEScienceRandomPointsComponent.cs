namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks a <see cref="CEScientificInterestComponent"/>-bearing entity as having its research
/// points randomized rather than fixed in yaml. On <see cref="Robust.Shared.GameObjects.MapInitEvent"/>,
/// rolls <see cref="RollCount"/> essence types (weighted towards low tiers, may repeat), each
/// granting a random amount within [<see cref="MinAmount"/>, <see cref="MaxAmount"/>], and writes
/// the result into the entity's <see cref="CEScientificInterestComponent.Points"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEScienceRandomPointsComponent : Component
{
    [DataField]
    public int RollCount = 3;

    [DataField]
    public int MinAmount = 1;

    [DataField]
    public int MaxAmount = 10;
}
