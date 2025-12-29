using Content.Client.CharacterInfo;
using Content.Client.Mind;
using Robust.Client.UserInterface.Controls;
using Content.Shared._CE.BlueText;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client._CE.BlueText;

public sealed class CEClientBlueTextController : UIController
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [UISystemDependency] private readonly MindSystem _mind = default!;

    private CEBlueTextMenu? _menu;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    public void OpenMenu(string blueText)
    {
        _menu?.Close();

        _menu = new CEBlueTextMenu();
        _menu.OpenCentered();
        _menu.Update(blueText);

        _menu.OnSubmitBlueText += OnBlueTextSave;
        _menu.OnClose += () => _menu = null;
    }

    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        if (!_cfg.GetCVar(CCVars.CEGameShowBlueText))
            return;

        if (!_mind.TryGetMind(ev.Entity, out var mind, out var mindComp))
            return;

        if (!_entManager.TryGetComponent<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        var btn = new Button
        {
            Text = Loc.GetString("ce-bluetext-open-button"),
            Margin = new Thickness(5)
        };

        btn.OnPressed += _ => OpenMenu(blueText.BlueText);

        ev.Controls.Add(btn);
    }

    private void OnBlueTextSave(string blueText)
    {
        var msg = new CEBlueTextSaveMessage
        {
            Text = blueText,
        };
        _net.ClientSendMessage(msg);
        _menu?.Close();
    }
}
