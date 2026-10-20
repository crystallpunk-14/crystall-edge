using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Trade.Rewards;

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CETradeReward
{
    /// <summary>
    /// Spawns the reward at <paramref name="coordinates"/>; the trade system then delivers it to the buyer.
    /// </summary>
    /// <param name="given">Receives the spawned entities.</param>
    public abstract void Give(IEntityManager entMan, MapCoordinates coordinates, List<EntityUid> given);

    /// <summary>
    /// Base value used for auto-pricing.
    /// </summary>
    public abstract double GetPrice(IEntityManager entMan);

    public abstract string GetName(IPrototypeManager protoMan);

    public abstract EntProtoId? GetPreview();
}
