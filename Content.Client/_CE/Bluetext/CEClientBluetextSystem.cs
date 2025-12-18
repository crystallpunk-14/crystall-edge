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

        SubscribeLocalEvent<CEBlueTextTrackerComponent, CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnGetCharacterInfoControls(Entity<CEBlueTextTrackerComponent> ent, ref CharacterInfoSystem.GetCharacterInfoControlsEvent args)
    {
        var btn = new Button
        {
            Text = Loc.GetString("ce-bluetext-open-button"),
            Margin = new Thickness(5)
        };

        btn.OnPressed += _ => { /* Intentionally does nothing for now */ };

        args.Controls.Add(btn);
    }
}
