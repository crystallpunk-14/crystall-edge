using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._CE.RoundPortal;

public sealed class CERoundPortalWindow : DefaultWindow
{
    public readonly Button LeaveButton;
    public readonly Button CancelButton;

    public CERoundPortalWindow()
    {
        var text = new RichTextLabel
        {
            MaxWidth = 400,
        };
        text.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString("ce-round-portal-window-text")));

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Children =
            {
                text,
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Align = AlignMode.Center,
                    Children =
                    {
                        (LeaveButton = new Button
                        {
                            Text = Loc.GetString("ce-round-portal-window-leave"),
                        }),
                        new Control
                        {
                            MinSize = new Vector2(20, 0),
                        },
                        (CancelButton = new Button
                        {
                            Text = Loc.GetString("quick-dialog-ui-cancel"),
                        }),
                    },
                },
            },
        });
    }
}
