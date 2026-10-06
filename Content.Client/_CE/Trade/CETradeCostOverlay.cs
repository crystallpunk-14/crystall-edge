using System.Numerics;
using Content.Client._CE.ResourceManager;
using Content.Client.Gameplay;
using Content.Shared._CE.Currency;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CE.Trade;

/// <summary>
/// Draws what the hovered trade offer costs next to the cursor: icon + amount per item and coin denomination.
/// Entries the local player can't afford are tinted red.
/// </summary>
public sealed partial class CETradeCostOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const float CoinIconSize = 24f;
    private const float ItemIconSize = 64f;
    private const float IconTextGap = 2f;
    private const float EntryGap = 8f;
    private const float OutlineOffset = 1f;
    private static readonly Vector2 CursorOffset = new(20f, 20f);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.3);

    private static readonly Color OutlineColor = Color.Black.WithAlpha(0.85f);
    private static readonly Color EnoughColor = Color.White;
    private static readonly Color NotEnoughColor = Color.FromHex("#dd4444");
    private static readonly Color ReceiveColor = Color.FromHex("#7fd27f");

    private static readonly Vector2[] OutlineOffsets =
    [
        new(-OutlineOffset, 0f),
        new(OutlineOffset, 0f),
        new(0f, -OutlineOffset),
        new(0f, OutlineOffset),
    ];

    private readonly SharedInteractionSystem _interaction;
    private readonly CESharedCurrencySystem _currency;
    private readonly CESharedTradeSystem _trade;

    private readonly Font _font;
    private readonly Texture?[] _coinIcons = new Texture?[4];
    private static readonly int[] CoinValues = [1000, 100, 10, 1];

    private readonly List<Entry> _entries = new();
    private readonly List<CEResourceIconRenderer> _costIcons = new();
    private EntityUid? _cachedOffer;
    private TimeSpan _nextRefresh;

    private readonly record struct Entry(Texture? Icon, CEResourceIconRenderer? Renderer, float IconSize, Color IconColor, string Text, Color TextColor);

    public CETradeCostOverlay()
    {
        IoCManager.InjectDependencies(this);

        _interaction = _entityManager.System<SharedInteractionSystem>();
        _currency = _entityManager.System<CESharedCurrencySystem>();
        _trade = _entityManager.System<CESharedTradeSystem>();

        var fontResource = _resourceCache.GetResource<FontResource>("/Fonts/_CE/Volkorn/VollkornSC-Bold.ttf");
        _font = new VectorFont(fontResource, 14);

        if (_resourceCache.TryGetResource<RSIResource>(new ResPath("/Textures/_CE/Interface/coins.rsi"), out var rsi))
        {
            string[] states = ["p", "g", "s", "c"];
            for (var i = 0; i < states.Length; i++)
            {
                _coinIcons[i] = rsi.RSI.TryGetState(states[i], out var state) ? state.Frame0 : null;
            }
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        if (GetHoveredOffer(args) is not { } offer || _player.LocalEntity is not { } player)
        {
            if (_cachedOffer is not null)
                ClearCostIcons();

            _cachedOffer = null;
            return;
        }

        if (_cachedOffer != offer.Owner)
        {
            _cachedOffer = offer.Owner;
            BuildCostIcons(offer);
            BuildEntries(offer, player);
            _nextRefresh = _timing.RealTime + RefreshInterval;
        }
        else if (_timing.RealTime >= _nextRefresh)
        {
            _nextRefresh = _timing.RealTime + RefreshInterval;
            BuildEntries(offer, player);
        }

        if (_entries.Count == 0)
            return;

        var handle = args.ScreenHandle;
        handle.SetTransform(Matrix3x2.Identity);

        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var pos = _inputManager.MouseScreenPosition.Position + CursorOffset * uiScale;
        var ascent = _font.GetAscent(uiScale);

        var rowHeight = 0f;
        foreach (var entry in _entries)
        {
            rowHeight = MathF.Max(rowHeight, entry.IconSize * uiScale);
        }

        foreach (var entry in _entries)
        {
            var iconSize = entry.IconSize * uiScale;
            var top = pos.Y + (rowHeight - iconSize) / 2f;

            var iconBox = UIBox2.FromDimensions(new Vector2(pos.X, top), new Vector2(iconSize, iconSize));
            if (entry.Renderer is { IsEmpty: false } renderer)
            {
                renderer.Draw(handle, iconBox);
                pos.X += iconSize + IconTextGap * uiScale;
            }
            else if (entry.Icon != null)
            {
                handle.DrawTextureRect(entry.Icon, iconBox, entry.IconColor);
                pos.X += iconSize + IconTextGap * uiScale;
            }

            if (entry.Text.Length > 0)
            {
                var textPos = new Vector2(pos.X, top + iconSize - ascent);
                foreach (var outline in OutlineOffsets)
                {
                    handle.DrawString(_font, textPos + outline * uiScale, entry.Text, uiScale, OutlineColor);
                }

                handle.DrawString(_font, textPos, entry.Text, uiScale, entry.TextColor);
                pos.X += handle.GetDimensions(_font, entry.Text, uiScale).X;
            }

            pos.X += EntryGap * uiScale;
        }
    }

    private void BuildEntries(Entity<CETradeOfferComponent> offer, EntityUid player)
    {
        _entries.Clear();

        if (!_proto.Resolve(offer.Comp.Offer, out var offerProto))
            return;

        var items = _trade.CollectTradeableItems(player);
        for (var i = 0; i < offerProto.Cost.Count; i++)
        {
            var cost = offerProto.Cost[i];
            var color = cost.CheckRequirement(_entityManager, _proto, items) ? EnoughColor : NotEnoughColor;
            var renderer = i < _costIcons.Count ? _costIcons[i] : null;

            _entries.Add(new Entry(null, renderer, ItemIconSize, Color.White, cost.GetRequirementAmount(), color));
        }

        if (offer.Comp.PayPrice > 0)
        {
            var color = _currency.GetPriceTotal(player) >= offer.Comp.PayPrice ? EnoughColor : NotEnoughColor;
            AddCoins(offer.Comp.PayPrice, color);
        }

        if (offer.Comp.ReceivePrice > 0)
        {
            _entries.Add(new Entry(null, null, CoinIconSize, Color.White, "»", ReceiveColor));
            AddCoins(offer.Comp.ReceivePrice, ReceiveColor);
        }
    }

    private void AddCoins(int amount, Color textColor)
    {
        for (var i = 0; i < CoinValues.Length; i++)
        {
            var count = amount / CoinValues[i];
            amount %= CoinValues[i];

            if (count > 0)
                _entries.Add(new Entry(_coinIcons[i], null, CoinIconSize, Color.White, count.ToString(), textColor));
        }
    }

    private void BuildCostIcons(Entity<CETradeOfferComponent> offer)
    {
        ClearCostIcons();

        if (!_proto.Resolve(offer.Comp.Offer, out var offerProto))
            return;

        foreach (var cost in offerProto.Cost)
        {
            var renderer = new CEResourceIconRenderer();
            renderer.SetLayers(cost.GetRequirementIcon(_entityManager, _proto));
            _costIcons.Add(renderer);
        }
    }

    private void ClearCostIcons()
    {
        foreach (var renderer in _costIcons)
        {
            renderer.Clear();
        }

        _costIcons.Clear();
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        ClearCostIcons();
    }

    private Entity<CETradeOfferComponent>? GetHoveredOffer(in OverlayDrawArgs args)
    {
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        var mouseMapPos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mouseMapPos.MapId != args.MapId)
            return null;

        if (screen.GetClickedEntity(mouseMapPos) is not { } target)
            return null;

        if (!_entityManager.TryGetComponent<CETradeOfferComponent>(target, out var offer))
            return null;

        if (_player.LocalEntity is not { } player || !_interaction.InRangeUnobstructed(player, target))
            return null;

        return (target, offer);
    }
}
