using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._CE.Recruitment;

public sealed class CERecruitmentWindow : DefaultWindow
{
    public readonly Button AcceptButton;
    public readonly Button DeclineButton;

    public CERecruitmentWindow()
    {
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
}
