using System.Numerics;
using Content.Shared._CE.Trade.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// Holds up to one offer entity per slot, parented to the table at the slot offset.
/// </summary>
[RegisterComponent, Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeTableComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CETradeShopPrototype> Shop;

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
