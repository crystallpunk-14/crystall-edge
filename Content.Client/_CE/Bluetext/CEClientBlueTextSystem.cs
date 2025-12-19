using Content.Client.CharacterInfo;
using Robust.Client.UserInterface.Controls;
using Content.Shared._CE.BlueText;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client._CE.BlueText;

public sealed class CEClientBlueTextSystem : CESharedBlueTextSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        if (!_cfg.GetCVar(CCVars.CEGameShowBlueText))
            return;

        if (!Mind.TryGetMind(ev.Entity, out var mind, out var mindComp))
            return;

        if (!TryComp<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        var btn = new Button
        {
            Text = Loc.GetString("ce-bluetext-open-button"),
            Margin = new Thickness(5)
        };

        btn.OnPressed += _ => RaiseNetworkEvent(new CEToggleBlueTextScreenEvent());

        ev.Controls.Add(btn);
    }
}
