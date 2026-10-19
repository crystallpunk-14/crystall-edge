using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Trade.Rewards;

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CETradeReward
{
    /// <summary>
    /// Hands the reward to the buyer.
    /// </summary>
    /// <param name="given">Receives the entities handed to the buyer.</param>
    public abstract void Give(IEntityManager entMan, EntityUid buyer, List<EntityUid> given);

    /// <summary>
    /// Base value used for auto-pricing.
    /// </summary>
    public abstract double GetPrice(IEntityManager entMan);

    public abstract string GetName(IPrototypeManager protoMan);

    public abstract EntProtoId? GetPreview();
}
