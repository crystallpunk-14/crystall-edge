using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// Holds up to one offer entity per slot, parented to the table at the slot offset. What goes into
/// a new offer is decided by whoever handles <see cref="CETradeTableFillOfferEvent"/> on the table,
/// e.g. <see cref="CETradeShopTableComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeTableComponent : Component
{
    [DataField(required: true)]
    public List<Vector2> Slots = new();

    [DataField]
    public EntProtoId OfferEntity = "CETradeOffer";

    /// <summary>
    /// Max distance in tiles an offer is randomly shifted from its slot position.
    /// </summary>
    [DataField]
    public float SlotJitter = 0.1f;
}

/// <summary>
/// Raised on a trade table right after a new offer entity is spawned into a free slot. A handler
/// fills the offer (see <see cref="CESharedTradeSystem.FillOffer"/>) and sets <see cref="Handled"/>;
/// an unhandled offer is deleted and the restock fails.
/// </summary>
[ByRefEvent]
public record struct CETradeTableFillOfferEvent(EntityUid Offer)
{
    public bool Handled;
}
