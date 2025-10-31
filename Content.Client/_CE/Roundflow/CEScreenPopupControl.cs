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

    private readonly Label _label;

    private string _title = string.Empty;
    private string _reason = string.Empty;

    private float _elapsedTime;
    private float _delayElapsedTime;

    public CEScreenPopupControl()
    {
        IoCManager.InjectDependencies(this);


        _label = new Label
        {
            Text = _title,
            FontOverride = _resourceCache.GetFont("/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf", 86),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontColorOverride = Color.Red,
        };

        AddChild(_label);
    }

    public void AnimationStart(CEScreenPopupShowEvent ev)
    {
        _title = ev.Title;
        _reason = ev.Reason;

        _label.Text = _title;

        _elapsedTime = 0;
        _delayElapsedTime = 0;
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

        _label.Modulate = Color.White.WithAlpha(MathHelper.Lerp(0f, 1f, _elapsedTime / FadeDuration));
    }
}
