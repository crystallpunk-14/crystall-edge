using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._CE.Recruitment;

public sealed partial class CERecruitmentWindow : DefaultWindow
{
    [Dependency] private IGameTiming _timing = default!;

    public readonly Button AcceptButton;
    public readonly Button DeclineButton;

    private readonly ProgressBar _timeLeftBar;

    private TimeSpan _endTime;
    private TimeSpan _duration;

    public CERecruitmentWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("ce-recruitment-window-title");

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Children =
            {
                new Label
                {
                    Text = Loc.GetString("ce-recruitment-window-prompt"),
                },
                new Label
                {
                    Text = Loc.GetString("ce-recruitment-window-warning"),
                    FontColorOverride = Color.Gray,
                },
                (_timeLeftBar = new ProgressBar
                {
                    MinValue = 0f,
                    MaxValue = 1f,
                    Value = 1f,
                    MinHeight = 8,
                    HorizontalExpand = true,
                }),
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Align = AlignMode.Center,
                    Children =
                    {
                        (AcceptButton = new Button
                        {
                            Text = Loc.GetString("ce-recruitment-window-accept"),
                        }),
                        new Control
                        {
                            MinSize = new Vector2(20, 0),
                        },
                        (DeclineButton = new Button
                        {
                            Text = Loc.GetString("ce-recruitment-window-decline"),
                        }),
                    },
                },
            },
        });
    }

    public void SetDeadline(TimeSpan endTime, TimeSpan duration)
    {
        _endTime = endTime;
        _duration = duration;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_duration <= TimeSpan.Zero)
            return;

        var left = (_endTime - _timing.CurTime) / _duration;
        _timeLeftBar.Value = Math.Clamp((float) left, 0f, 1f);
    }
}
