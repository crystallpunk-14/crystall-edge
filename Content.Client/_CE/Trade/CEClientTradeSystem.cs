using Content.Client.Stack;
using Content.Client.Storage.Systems;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._CE.Trade;

public sealed partial class CEClientTradeSystem : CESharedTradeSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private ItemCounterSystem _counter = default!;

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<CETradeOfferComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!Proto.Resolve(ent.Comp.Offer, out var offer) ||
            GetPreview(offer, ent.Comp.ReceivePrice, out var stackCount) is not { } preview)
        {
            _sprite.SetVisible(ent.Owner, false);
            return;
        }

        var temp = Spawn(preview, MapCoordinates.Nullspace);
        _sprite.CopySprite(temp, ent.Owner);

        if (TryComp<StackComponent>(temp, out var stack) && stack.LayerStates.Count > 0)
            ApplyStackVisuals(ent.Owner, (temp, stack), stackCount ?? stack.Count);

        Del(temp);

        _sprite.SetScale(ent.Owner, ent.Comp.PreviewScale);
        _sprite.SetVisible(ent.Owner, true);
    }

    /// <summary>
    /// Draws the target sprite the way the source stack would look with the given count.
    /// </summary>
    private void ApplyStackVisuals(EntityUid target, Entity<StackComponent> source, int count)
    {
        var maxCount = _stack.GetMaxCount(source.Comp);

        if (source.Comp.LayerFunction != StackLayerFunction.None)
            _stack.ApplyLayerFunction(source, ref count, ref maxCount);

        if (source.Comp.IsComposite)
            _counter.ProcessCompositeSprite(target, count, maxCount, source.Comp.LayerStates);
        else
            _counter.ProcessOpaqueSprite(target, source.Comp.BaseLayer, count, maxCount, source.Comp.LayerStates);
    }
}
