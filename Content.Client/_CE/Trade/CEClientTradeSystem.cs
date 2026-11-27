using Content.Client.Guidebook.Components;
using Content.Client.Stack;
using Content.Client.Storage.Systems;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Trade;

public sealed partial class CEClientTradeSystem : CESharedTradeSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private ItemCounterSystem _counter = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<CETradeOfferComponent> _offers = default!;

    private readonly HashSet<EntityUid> _outdatedOffers = new();

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<CETradeOfferComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Guides.Count > 0)
            EnsureComp<GuideHelpComponent>(ent).Guides = new(ent.Comp.Guides);

        // Redrawn next frame: while a server state is being applied, a freshly spawned preview gets no appearance data
        _outdatedOffers.Add(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var uid in _outdatedOffers)
        {
            if (_offers.TryComp(uid, out var offer))
                DrawOffer((uid, offer));
        }

        _outdatedOffers.Clear();
    }

    /// <summary>
    /// Copies the sprite of the offer preview, with its visuals applied, onto the offer.
    /// </summary>
    private void DrawOffer(Entity<CETradeOfferComponent> ent)
    {
        if (GetPreview(ent.Comp, out var stackCount) is not { } preview)
        {
            _sprite.SetVisible(ent.Owner, false);
            return;
        }

        // Map init fills the appearance data, which is normally drawn only on the next frame
        var temp = Spawn(preview);
        _appearance.OnChangeData(temp, CompOrNull<SpriteComponent>(temp));
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
