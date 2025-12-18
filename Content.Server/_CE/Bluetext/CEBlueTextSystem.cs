using Content.Server.Antag;
using Content.Server.Mind;
using Content.Shared._CE.Bluetext;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._CE.Bluetext;

public sealed class CEBlueTextSystem : CESharedBlueTextSystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEBlueTextRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagAttached);

        SubscribeNetworkEvent<CEToggleBluetextScreenEvent>(OnToggleBluetext);
    }

    private void OnAntagAttached(Entity<CEBlueTextRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mind, out var mindComp))
            return;

        EnsureComp<CEBlueTextTrackerComponent>(mind);
    }

    private void OnToggleBluetext(CEToggleBluetextScreenEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {Valid: true} ent)
            return;

        if (!_mind.TryGetMind(ent, out var mind, out var mindComp))
            return;

        if (!TryComp<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _userInterface.TryToggleUi(ent, CEBluetextUIKey.Key, actor.PlayerSession);

        var state = new CEBluetextBuiState(blueText.BlueText);
        _userInterface.SetUiState(ent, CEBluetextUIKey.Key, state);
    }
}
