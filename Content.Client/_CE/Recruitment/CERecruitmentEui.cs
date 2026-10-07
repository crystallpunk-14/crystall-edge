using Content.Client.Eui;
using Content.Shared._CE.Recruitment;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._CE.Recruitment;

[UsedImplicitly]
public sealed class CERecruitmentEui : BaseEui
{
    private readonly CERecruitmentWindow _window;

    public CERecruitmentEui()
    {
        _window = new CERecruitmentWindow();

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new CERecruitmentChoiceMessage(true));
            _window.Close();
        };

        _window.DeclineButton.OnPressed += _ =>
        {
            SendMessage(new CERecruitmentChoiceMessage(false));
            _window.Close();
        };

        // Closing the window is the same as declining - the server ignores it once answered.
        _window.OnClose += () => SendMessage(new CERecruitmentChoiceMessage(false));
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
