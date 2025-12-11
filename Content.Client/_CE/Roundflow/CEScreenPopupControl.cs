using Content.Client.Resources;
using Content.Shared._CE.Roundflow;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._CE.RoundFlow;

public sealed class CEScreenPopupControl : Control
{
    private const float FadeDuration = 4f;
    private const float DelayTime = 3f;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly FontTagHijackHolder _fontHijack = default!;

    public event Action? OnAnimationEnd;

    private readonly BoxContainer _vbox;
    private readonly RichTextLabel _titleLabel;
    private readonly RichTextLabel _reasonLabel;

    private float _elapsedTime;
    private float _delayElapsedTime;

    public CEScreenPopupControl()
    {
        IoCManager.InjectDependencies(this);

        _titleLabel = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(10),
        };

        _reasonLabel = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(10),
        };

        _vbox = new BoxContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        _vbox.AddChild(_titleLabel);
        _vbox.AddChild(_reasonLabel);
        AddChild(_vbox);

        _vbox.Margin = new Thickness(0, 120, 0, 0);

        // Register fonts for markup [font="_ce_popup_title"] and [font="_ce_popup_reason"].
        var previousHijack = _fontHijack.Hijack;
        _fontHijack.Hijack = (protoId, size) =>
        {
            // protoId implicitly converts to string
            if (protoId == "_ce_popup_title")
            {
                return _resourceCache.GetFont("/Fonts/_CE/Volkorn/VollkornSC-Bold.ttf", size);
            }

            if (protoId == "_ce_popup_reason")
            {
                return _resourceCache.GetFont("/Fonts/_CE/Volkorn/VollkornSC-Regular.ttf", size);
            }

            return previousHijack?.Invoke(protoId, size);
        };

        // Notify UI that hijack changed so existing rich text controls update.
        _fontHijack.HijackUpdated();
    }

    public void AnimationStart(CEScreenPopupShowEvent ev)
    {
        // Wrap with font tags so we use our CE fonts while still allowing [color=...] and other markup.
        var titleMarkup = $"[font=\"_ce_popup_title\" size=64]{ev.Title}[/font]";
        var reasonMarkup = $"[font=\"_ce_popup_reason\" size=36]{ev.Description}[/font]";

        // Use permissive parsing so invalid markup is treated as text.
        _titleLabel.SetMessage(FormattedMessage.FromMarkupPermissive(titleMarkup), null, Color.White);
        _reasonLabel.SetMessage(FormattedMessage.FromMarkupPermissive(reasonMarkup), null, Color.White);

        _elapsedTime = 0f;
        _delayElapsedTime = 0f;

        Modulate = Color.White.WithAlpha(0f);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_elapsedTime >= FadeDuration)
        {
            if (_delayElapsedTime < DelayTime)
            {
                _delayElapsedTime += args.DeltaSeconds;
                return;
            }

            OnAnimationEnd?.Invoke();
            return;
        }

        _elapsedTime += args.DeltaSeconds;
        var alpha = MathHelper.Lerp(0f, 1f, _elapsedTime / FadeDuration);
        Modulate = Color.White.WithAlpha(alpha);
    }
}
