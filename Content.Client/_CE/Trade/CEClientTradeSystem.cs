using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._CE.Trade;

public sealed partial class CEClientTradeSystem : CESharedTradeSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<CETradeOfferComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!Proto.Resolve(ent.Comp.Offer, out var offer) || GetPreview(offer, ent.Comp.ReceivePrice) is not { } preview)
        {
            _sprite.SetVisible(ent.Owner, false);
            return;
        }

        var temp = Spawn(preview, MapCoordinates.Nullspace);
        _sprite.CopySprite(temp, ent.Owner);
        Del(temp);

        _sprite.SetScale(ent.Owner, ent.Comp.PreviewScale);
        _sprite.SetVisible(ent.Owner, true);
    }
}
