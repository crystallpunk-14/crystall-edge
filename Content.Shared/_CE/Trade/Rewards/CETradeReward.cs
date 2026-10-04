using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Rewards;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CETradeReward
{
    /// <summary>
    /// Hands the reward to the buyer.
    /// </summary>
    public abstract void Give(IEntityManager entMan, EntityUid buyer);

    /// <summary>
    /// Base value used for auto-pricing.
    /// </summary>
    public abstract double GetPrice(IEntityManager entMan);

    public abstract string GetName(IPrototypeManager protoMan);

    public abstract EntProtoId? GetPreview();
}
