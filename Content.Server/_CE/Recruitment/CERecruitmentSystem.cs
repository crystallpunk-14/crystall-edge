using Content.Server._CE.Roles;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared._CE.Recruitment;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CE.Recruitment;

/// <summary>
/// Handles <see cref="CERecruitActionEvent"/>: invites a player into a secret department, and
/// switches their secret role if they accept.
/// </summary>
public sealed partial class CERecruitmentSystem : CESharedRecruitmentSystem
{
    [Dependency] private CESecretRoleSelectionSystem _secretRoles = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    protected override LocId? GetServerInvalidReason(EntityUid target)
    {
        if (!HasComp<ActorComponent>(target))
            return "ce-recruitment-invalid-target";

        if (HasComp<CERecruitmentInviteComponent>(target))
            return "ce-recruitment-already-invited";

        return null;
    }

    [SubscribeLocalEvent]
    private void OnRecruitAction(CERecruitActionEvent args)
    {
        if (args.Handled ||
            !TryComp<CERecruitmentActionComponent>(args.Action, out var recruitment) ||
            !TryComp<ActorComponent>(args.Target, out var actor))
        {
            return;
        }

        var target = args.Target;
        var invite = AddComp<CERecruitmentInviteComponent>(target);
        invite.Recruiter = args.Performer;
        invite.Role = recruitment.Role;
        invite.EndTime = _timing.CurTime + recruitment.Timeout;
        invite.Eui = new CERecruitmentEui(target, this, invite.EndTime, recruitment.Timeout);

        _eui.OpenEui(invite.Eui, actor.PlayerSession);
        args.Handled = true;
    }

    /// <summary>
    /// Answers a pending invite on <paramref name="target"/> - no-op if there is none.
    /// </summary>
    /// <param name="closeEui">Whether to close the dialog too - false when the dialog itself is
    /// the one answering or closing.</param>
    public void ResolveInvite(EntityUid target, bool accepted, bool closeEui = true)
    {
        if (!TryComp<CERecruitmentInviteComponent>(target, out var invite))
            return;

        var recruiter = invite.Recruiter;
        var role = invite.Role;
        var eui = invite.Eui;
        RemComp(target, invite);

        if (closeEui && eui is { IsShutDown: false })
            eui.Close();

        if (accepted &&
            (!TryComp<ActorComponent>(target, out var actor) ||
             !_secretRoles.TrySetSecretRole(actor.PlayerSession, role, out _, removeSkills: false)))
        {
            accepted = false;
        }

        if (!TryComp<ActorComponent>(recruiter, out var recruiterActor))
            return;

        var message = Loc.GetString(
            accepted ? "ce-recruitment-accepted" : "ce-recruitment-declined",
            ("name", Name(target)));
        _chat.ChatMessageToOne(ChatChannel.Server, message, message, EntityUid.Invalid, false, recruiterActor.PlayerSession.Channel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Collected first - resolving removes the very component being enumerated.
        var expired = new List<EntityUid>();
        var query = EntityQueryEnumerator<CERecruitmentInviteComponent>();
        while (query.MoveNext(out var uid, out var invite))
        {
            if (_timing.CurTime < invite.EndTime &&
                _mobState.IsAlive(uid) &&
                HasComp<ActorComponent>(uid))
            {
                continue;
            }

            expired.Add(uid);
        }

        foreach (var uid in expired)
        {
            ResolveInvite(uid, false);
        }
    }
}
