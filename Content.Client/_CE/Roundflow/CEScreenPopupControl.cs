using Content.Client.Resources;
using Content.Shared._CE.Roundflow;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._CE.RoundFlow;

public sealed class CEScreenPopupControl : Control
{
    private const float FadeDuration = 4f;
    private const float DelayTime = 3f;

    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public event Action? OnAnimationEnd;

    private readonly BoxContainer _vbox;
    private readonly Label _titleLabel;
    private readonly Label _reasonLabel;

    private float _elapsedTime;
    private float _delayElapsedTime;

    public CEScreenPopupControl()
    {
        IoCManager.InjectDependencies(this);

        _titleLabel = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontOverride = _resourceCache.GetFont("/Fonts/_CE/Volkorn/VollkornSC-Bold.ttf", 64),
            FontColorOverride = Color.White
        };

        _reasonLabel = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontOverride = _resourceCache.GetFont("/Fonts/_CE/Volkorn/VollkornSC-Regular.ttf", 36),
            FontColorOverride = Color.White
        };

        _vbox = new BoxContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        _vbox.AddChild(_titleLabel);
        _vbox.AddChild(_reasonLabel);
        AddChild(_vbox);

        _vbox.Margin = new Thickness(0, 60, 0, 0);
    }

    public void AnimationStart(CEScreenPopupShowEvent ev)
    {
        _titleLabel.Text = ev.Title;
        _reasonLabel.Text = ev.Description;

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
