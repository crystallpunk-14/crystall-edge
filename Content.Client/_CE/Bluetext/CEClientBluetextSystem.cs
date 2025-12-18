using Content.Client.CharacterInfo;
using Robust.Client.UserInterface.Controls;
using Content.Shared._CE.Bluetext;

namespace Content.Client._CE.Bluetext;

public sealed class CEClientBluetextSystem : CESharedBlueTextSystem
{
    [Dependency] private readonly CharacterInfoSystem _characterInfo = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        if (!Mind.TryGetMind(ev.Entity, out var mind, out var mindComp))
            return;

        if (!TryComp<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        var btn = new Button
        {
            Text = Loc.GetString("ce-bluetext-open-button"),
            Margin = new Thickness(5)
        };

        btn.OnPressed += _ => { /* Intentionally does nothing for now */ };

        ev.Controls.Add(btn);
    }
}
