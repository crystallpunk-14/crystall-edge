using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Shared._CE.BlueText;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.Server._CE.BlueText;

public sealed class CEBlueTextSystem : CESharedBlueTextSystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEBlueTextRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagAttached);

        _net.RegisterNetMessage<CEBlueTextSaveMessage>(OnSaveBlueText);
    }

    private void OnSaveBlueText(CEBlueTextSaveMessage message)
    {
        if (!_mind.TryGetMind(message.MsgChannel.UserId, out var mind))
            return;

        if (!TryComp<CEBlueTextTrackerComponent>(mind, out var blueText))
            return;

        blueText.BlueText = message.Text.Length > MaxTextLength ? message.Text[..MaxTextLength] : message.Text;
        Dirty(mind.Value, blueText);
        _adminLog.Add(LogType.Mind, $"{ToPrettyString(mind.Value.Comp.OwnedEntity)} has updated their blue text to: \"{blueText.BlueText}\"");
    }

    private void OnAntagAttached(Entity<CEBlueTextRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mind, out var mindComp))
            return;

        EnsureComp<CEBlueTextTrackerComponent>(mind);
    }
}
