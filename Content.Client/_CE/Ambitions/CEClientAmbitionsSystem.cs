using Content.Client.CharacterInfo;
using Content.Shared._CE.Ambitions;
using Content.Shared._CE.Ambitions.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._CE.Ambitions;

public sealed class CEClientAmbitionsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEAmbitionsSetupComponent, CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnGetCharacterInfoControls(Entity<CEAmbitionsSetupComponent> ent, ref CharacterInfoSystem.GetCharacterInfoControlsEvent args)
    {
        var btn = new Button
        {
            Text = Loc.GetString("ce-ambitions-ui-button-open"),
            Margin = new Thickness(5)
        };

        btn.OnPressed += _ => OnOpenAmbitionsUI(ent);

        args.Controls.Add(btn);
    }

    private void OnOpenAmbitionsUI(Entity<CEAmbitionsSetupComponent> entity)
    {
        var session = _playerManager.LocalSession;
        if (session == null)
            return;

        RaiseNetworkEvent(new CEToggleAmbitionsScreenEvent(), session);
    }
}
