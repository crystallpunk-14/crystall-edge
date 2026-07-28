using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CE.UserInterface.Systems.Chat.Widgets;

/// <summary>
/// Chat box for the Default screen: no background, anchored bottom-right, fixed size.
/// Collapsed (unfocused) it only shows recent messages, each fading out a fixed time after
/// it was received; the input row (channel selector, line edit, filter button) is hidden.
/// Pressing the chat hotkey (T) expands it to the full history and the input row.
/// </summary>
public sealed class CEChatBox : ChatBox
{
    private const float FadeHoldSeconds = 5f;
    private const float FadeDurationSeconds = 1f;

    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ChatUIController _ceController;
    private readonly ScrollContainer _scrollContainer;
    private readonly BoxContainer _messagesBox;
    private readonly List<CEChatEntry> _entries = new();

    private bool _expanded;

    public CEChatBox()
    {
        IoCManager.InjectDependencies(this);

        _ceController = UserInterfaceManager.GetUIController<ChatUIController>();

        // Pull the input row out before hiding the stock panel, then hide the panel
        // (background + built-in output list) since we render our own list below.
        ChatInput.Orphan();
        ChatWindowPanel.Visible = false;

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        AddChild(root);

        _messagesBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        // Spacer that eats leftover space so short message lists hug the bottom instead of the top.
        _messagesBox.AddChild(new Control { VerticalExpand = true });

        _scrollContainer = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
        };
        _scrollContainer.AddChild(_messagesBox);
        root.AddChild(_scrollContainer);

        root.AddChild(ChatInput);

        ChatInput.Input.OnFocusEnter += _ => SetExpanded(true);
        ChatInput.Input.OnFocusExit += _ => SetExpanded(false);
        ChatInput.FilterButton.Popup.OnChannelFilter += (_, _) => RebuildFromHistory();

        _ceController.MessageAdded += OnCEMessageAdded;

        SetExpanded(false);
        RebuildFromHistory();
    }

    public override void Repopulate()
    {
        RebuildFromHistory();
    }

    private void OnCEMessageAdded(ChatMessage msg)
    {
        if (!ChatInput.FilterButton.Popup.IsActive(msg.Channel))
            return;

        AddEntry(msg);
        _scrollContainer.VScrollTarget = float.MaxValue;
    }

    private void RebuildFromHistory()
    {
        _messagesBox.DisposeAllChildren();
        _messagesBox.AddChild(new Control { VerticalExpand = true });
        _entries.Clear();

        foreach (var (_, msg) in _ceController.History)
        {
            if (ChatInput.FilterButton.Popup.IsActive(msg.Channel))
                AddEntry(msg);
        }

        _scrollContainer.VScrollTarget = float.MaxValue;
    }

    private void AddEntry(ChatMessage msg)
    {
        var color = msg.MessageColorOverride ?? msg.Channel.TextColor();

        var formatted = new FormattedMessage(3);
        formatted.PushColor(color);
        formatted.AddMarkupOrThrow(msg.WrappedMessage);
        formatted.Pop();

        var label = new RichTextLabel { HorizontalExpand = true };
        label.SetMessage(formatted, tagsAllowed: null);

        _messagesBox.AddChild(label);
        _entries.Add(new CEChatEntry(label, _timing.RealTime));

        if (_expanded)
            label.Modulate = Color.White;
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;

        ChatInput.Modulate = expanded ? Color.White : Color.White.WithAlpha(0f);
        ChatInput.MouseFilter = expanded ? MouseFilterMode.Stop : MouseFilterMode.Ignore;
        _scrollContainer.MouseFilter = expanded ? MouseFilterMode.Pass : MouseFilterMode.Ignore;

        if (expanded)
        {
            foreach (var entry in _entries)
            {
                entry.Label.Modulate = Color.White;
                entry.Label.Visible = true;
            }
        }
        else
        {
            _scrollContainer.VScrollTarget = float.MaxValue;
            UpdateFades();
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_expanded)
            UpdateFades();
    }

    private void UpdateFades()
    {
        var now = _timing.RealTime;

        foreach (var entry in _entries)
        {
            var age = (now - entry.ReceivedAt).TotalSeconds;
            var alpha = age <= FadeHoldSeconds
                ? 1f
                : (float) Math.Clamp(1.0 - (age - FadeHoldSeconds) / FadeDurationSeconds, 0.0, 1.0);

            entry.Label.Visible = alpha > 0f;
            if (alpha > 0f)
                entry.Label.Modulate = Color.White.WithAlpha(alpha);
        }
    }

    private sealed class CEChatEntry
    {
        public readonly RichTextLabel Label;
        public readonly TimeSpan ReceivedAt;

        public CEChatEntry(RichTextLabel label, TimeSpan receivedAt)
        {
            Label = label;
            ReceivedAt = receivedAt;
        }
    }
}
