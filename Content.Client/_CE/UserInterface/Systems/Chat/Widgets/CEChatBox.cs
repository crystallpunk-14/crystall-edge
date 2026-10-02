using System.Numerics;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Chat;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CE.UserInterface.Systems.Chat.Widgets;

/// <summary>
/// Chat box for the minimalist screen: anchored bottom-left, fixed size.
/// Collapsed it only shows recent messages without a background, each fading out a fixed time
/// after it was received; the input row (channel selector, line edit, filter button) is invisible.
/// Hovering the input row or focusing the line edit (T) expands it to the stock chat panel with
/// the full history; the panel stays open while the cursor is anywhere over it.
/// </summary>
public sealed partial class CEChatBox : ChatBox
{
    private const float FadeHoldSeconds = 10f;
    private const float FadeDurationSeconds = 1f;

    // Upper bound on simultaneously shown recent messages. Anything above this would be clipped
    // off the top of the box anyway, and it keeps a chat burst from piling up controls.
    private const int MaxFadeEntries = 30;

    [Dependency] private IGameTiming _timing = default!;

    private readonly ChatUIController _ceController;
    private readonly CEChatFadeStack _fadeStack;
    private readonly VScrollBar? _contentsScrollBar;
    private readonly List<CEChatEntry> _entries = new();

    private bool _focused;
    private bool _expanded;

    public CEChatBox()
    {
        IoCManager.InjectDependencies(this);

        _ceController = UserInterfaceManager.GetUIController<ChatUIController>();

        // Invisible chat must not eat clicks aimed at the world behind it - only the input row
        // and the expanded panel take the mouse.
        MouseFilter = MouseFilterMode.Ignore;

        // Expanded view reuses the stock panel + OutputPanel: it only lays out and draws the
        // visible lines and handles scrolling / sticking to the bottom on its own. The input row is
        // pulled out of it so that it keeps its place (and hover area) while the panel is hidden.
        ChatInput.Orphan();
        ChatWindowPanel.Orphan();
        ChatWindowPanel.MouseFilter = MouseFilterMode.Stop;
        ChatInput.MouseFilter = MouseFilterMode.Stop;

        foreach (var child in Contents.Children)
        {
            if (child is VScrollBar bar)
                _contentsScrollBar = bar;
        }

        _fadeStack = new CEChatFadeStack();

        // Panel and fade stack overlap: plain Control stretches every child over its whole area.
        var messagesArea = new Control
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        messagesArea.AddChild(ChatWindowPanel);
        messagesArea.AddChild(_fadeStack);

        // No separation: a gap between the panel and the input row would let the hover drop while
        // the cursor moves from the input up to the history.
        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 0,
        };
        root.AddChild(messagesArea);
        root.AddChild(ChatInput);
        AddChild(root);

        ChatInput.Input.OnFocusEnter += _ => _focused = true;
        ChatInput.Input.OnFocusExit += _ => _focused = false;
        ChatInput.FilterButton.Popup.OnChannelFilter += OnCEChannelFilter;

        _ceController.MessageAdded += OnCEMessageAdded;

        ApplyExpanded(false);
        Repopulate();
    }

    public override void Repopulate()
    {
        base.Repopulate();
        RebuildFades();
    }

    private void OnCEChannelFilter(ChatChannel channel, bool active)
    {
        RebuildFades();
    }

    private void OnCEMessageAdded(ChatMessage msg)
    {
        if (!ChatInput.FilterButton.Popup.IsActive(msg.Channel))
            return;

        AddFadeEntry(msg, _timing.RealTime);
    }

    /// <summary>
    /// Refills the recent-messages stack from history, keeping each message's real age so a
    /// rebuild (filter change, admin erase, screen switch) doesn't resurface old messages.
    /// </summary>
    private void RebuildFades()
    {
        foreach (var entry in _entries)
        {
            entry.Label.Orphan();
        }
        _entries.Clear();

        var history = _ceController.History;
        var lifetime = TimeSpan.FromSeconds(FadeHoldSeconds + FadeDurationSeconds);

        // History is in arrival order - walk back only as far as messages are still alive.
        var start = history.Count;
        while (start > 0 && GetTickAge(history[start - 1].Tick) < lifetime)
        {
            start--;
        }

        var now = _timing.RealTime;
        for (var i = start; i < history.Count; i++)
        {
            var (tick, msg) = history[i];
            if (ChatInput.FilterButton.Popup.IsActive(msg.Channel))
                AddFadeEntry(msg, now - GetTickAge(tick));
        }
    }

    private TimeSpan GetTickAge(GameTick tick)
    {
        var curTick = _timing.CurTick.Value;
        var ticks = curTick > tick.Value ? curTick - tick.Value : 0;
        return _timing.TickPeriod * ticks;
    }

    private void AddFadeEntry(ChatMessage msg, TimeSpan receivedAt)
    {
        var color = msg.MessageColorOverride ?? msg.Channel.TextColor();

        var formatted = new FormattedMessage(3);
        formatted.PushColor(color);
        formatted.AddMarkupOrThrow(msg.WrappedMessage);
        formatted.Pop();

        var label = new RichTextLabel
        {
            HorizontalExpand = true,
            OutlineColorOverride = TextOutline.Default.Color,
        };
        label.SetMessage(formatted, tagsAllowed: null);

        _fadeStack.AddChild(label);
        _entries.Add(new CEChatEntry(label, receivedAt));

        while (_entries.Count > MaxFadeEntries)
        {
            RemoveOldestEntry();
        }

        UpdateFade(_entries[^1], _timing.RealTime);
    }

    private void RemoveOldestEntry()
    {
        _entries[0].Label.Orphan();
        _entries.RemoveAt(0);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var expanded = _focused || IsChatHovered() || IsPopupOpen();
        if (expanded != _expanded)
            ApplyExpanded(expanded);

        UpdateFadeStackMargin();
        UpdateFades();
    }

    // Lines must wrap at exactly the same width collapsed and expanded, or words jump between
    // lines when the chat opens. OutputPanel lays text out inside its own margin, minus the width of
    // its scrollbar (reserved even while the bar is hidden) - mirror both on the fade stack.
    private void UpdateFadeStackMargin()
    {
        var scrollBarWidth = 0f;
        if (_contentsScrollBar != null)
        {
            // The panel may not have been laid out yet (hidden until the first expand); measuring
            // is cached, so this is free once the bar's size is known.
            _contentsScrollBar.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            scrollBarWidth = _contentsScrollBar.DesiredSize.X;
        }

        var contentsMargin = Contents.Margin;
        var margin = new Thickness(
            contentsMargin.Left,
            contentsMargin.Top,
            contentsMargin.Right + scrollBarWidth,
            contentsMargin.Bottom);

        if (_fadeStack.Margin != margin)
            _fadeStack.Margin = margin;
    }

    // Uses the UI's own hover target, so a window or popup covering the chat blocks it.
    // Collapsed, only the input row opens the chat; expanded, the whole panel keeps it open so
    // the history can be scrolled.
    private bool IsChatHovered()
    {
        var target = _expanded ? (Control) this : ChatInput;

        for (var control = UserInterfaceManager.CurrentlyHovered; control != null; control = control.Parent)
        {
            if (control == target)
                return true;
        }

        return false;
    }

    // The channel selector and filter popups live in the popup root, outside of this control -
    // keep the chat open while one of them is in use.
    private bool IsPopupOpen()
    {
        return ChatInput.FilterButton.Popup.Visible || ChatInput.ChannelSelector.Popup.Visible;
    }

    private void ApplyExpanded(bool expanded)
    {
        _expanded = expanded;

        ChatWindowPanel.Visible = expanded;
        _fadeStack.Visible = !expanded;
        ChatInput.Modulate = expanded ? Color.White : Color.White.WithAlpha(0f);

        if (expanded)
            UserInterfaceManager.DeferAction(SnapHistoryToBottom);
    }

    // Deferred so it runs after this frame's layout, once the panel has its real size and range.
    // Sets the value directly instead of the animated target: the scrollbar didn't tick while
    // hidden, so animating would visibly sweep through everything received since the last opening.
    private void SnapHistoryToBottom()
    {
        if (Disposed || !_expanded)
            return;

        Contents.ScrollToBottom();

        if (_contentsScrollBar != null)
            _contentsScrollBar.Value = _contentsScrollBar.ValueTarget;
    }

    private void UpdateFades()
    {
        var now = _timing.RealTime;
        var lifetime = FadeHoldSeconds + FadeDurationSeconds;

        // Entries are in arrival order, so the fully faded ones are always at the front.
        while (_entries.Count > 0 && (now - _entries[0].ReceivedAt).TotalSeconds >= lifetime)
        {
            RemoveOldestEntry();
        }

        foreach (var entry in _entries)
        {
            UpdateFade(entry, now);
        }
    }

    private static void UpdateFade(CEChatEntry entry, TimeSpan now)
    {
        var age = (now - entry.ReceivedAt).TotalSeconds;
        var alpha = age <= FadeHoldSeconds
            ? 1f
            : (float) Math.Clamp(1.0 - (age - FadeHoldSeconds) / FadeDurationSeconds, 0.0, 1.0);

        entry.Label.Modulate = Color.White.WithAlpha(alpha);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _ceController.MessageAdded -= OnCEMessageAdded;
        ChatInput.FilterButton.Popup.OnChannelFilter -= OnCEChannelFilter;
    }

    private readonly record struct CEChatEntry(RichTextLabel Label, TimeSpan ReceivedAt);

    /// <summary>
    /// Stacks children upwards from its bottom edge. Reports no desired size of its own, so
    /// when there are more lines than fit, the oldest ones get clipped off the top instead of the
    /// newest ones being pushed out of the bottom.
    /// </summary>
    private sealed class CEChatFadeStack : Control
    {
        public CEChatFadeStack()
        {
            HorizontalExpand = true;
            VerticalExpand = true;
            RectClipContent = true;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var constraint = new Vector2(availableSize.X, float.PositiveInfinity);
            foreach (var child in Children)
            {
                child.Measure(constraint);
            }

            return Vector2.Zero;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var bottom = finalSize.Y;
            for (var i = ChildCount - 1; i >= 0; i--)
            {
                var child = GetChild(i);
                var height = child.DesiredSize.Y;
                child.Arrange(UIBox2.FromDimensions(new Vector2(0, bottom - height), new Vector2(finalSize.X, height)));
                bottom -= height;
            }

            return finalSize;
        }
    }
}
