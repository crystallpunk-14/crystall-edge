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

    [DataField]
    public List<Vector2> Slots = new()
    {
        new Vector2(-0.22f, 0.22f),
        new Vector2(0.22f, 0.22f),
        new Vector2(-0.22f, -0.22f),
        new Vector2(0.22f, -0.22f),
    };

    [DataField]
    public EntProtoId OfferEntity = "CETradeOffer";
}
