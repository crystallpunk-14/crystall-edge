using Content.Server.Antag;
using Content.Server.Mind;
using Content.Shared._CE.BlueText;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._CE.BlueText;

public sealed class CEBlueTextSystem : CESharedBlueTextSystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEBlueTextRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagAttached);

        SubscribeNetworkEvent<CEToggleBlueTextScreenEvent>(OnToggleBlueText);
        SubscribeLocalEvent<ActorComponent, CEBlueTextSubmitMessage>(OnSubmitBlueText);
    }

    private void OnAntagAttached(Entity<CEBlueTextRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mind, out var mindComp))
            return;

        EnsureComp<CEBlueTextTrackerComponent>(mind);
    }

    private void OnToggleBlueText(CEToggleBlueTextScreenEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {Valid: true} ent)
            return;

        if (!_mind.TryGetMind(ent, out var mind, out var mindComp))
            return;

        if (!TryComp<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        if (!_cfg.GetCVar(CCVars.CEGameShowBlueText))
            return;

        _userInterface.TryToggleUi(ent, CEBlueTextUIKey.Key, actor.PlayerSession);

        var state = new CEBlueTextBuiState(blueText.BlueText);
        _userInterface.SetUiState(ent, CEBlueTextUIKey.Key, state);
    }

    private void OnSubmitBlueText(Entity<ActorComponent> ent, ref CEBlueTextSubmitMessage args)
    {
        if (!_mind.TryGetMind(ent, out var mind, out var mindComp))
            return;

        if (!TryComp<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        var text = args.Text;

        if (text.Length > MaxTextLength)
            text = text[..MaxTextLength];

        blueText.BlueText = text;

        var state = new CEBlueTextBuiState(blueText.BlueText);
        _userInterface.SetUiState(ent.Owner, CEBlueTextUIKey.Key, state);
    }
}
